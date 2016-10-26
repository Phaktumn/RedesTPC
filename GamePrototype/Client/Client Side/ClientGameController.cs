﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Network;
using Core.Core;

namespace Client.Client_Side
{
    class ClientGameController
    {
        private GameModes clientGameState { get; set; }
        private ClientController controller { get; }

        private Color receivedMessageColor { get; } = Color.Aqua;

        private Menu abMenu;
        private Menu endGameMenu;
        private Menu mainMenu;

        private bool inPlay = true;
        private bool waitingResults = false;
        private bool showMainMenu = true;

        private string ChosenName = null;

        public ClientGameController()
        {
            controller = new ClientController();
            controller.MessageReceivedEvent += ControllerOnMessageReceivedEvent;

            abMenu = new Menu(5);
            var option = new MenuOption { OptionString = Abilities.Scissors.ToString() };
            option.ExecuteEvent += AbilityMenuEvent;
            abMenu.AddOption(option, "1");

            var option1 = new MenuOption { OptionString = Abilities.Paper.ToString() };
            option1.ExecuteEvent += AbilityMenuEvent;
            abMenu.AddOption(option1, "2");

            var option2 = new MenuOption { OptionString = Abilities.Rock.ToString() };
            option2.ExecuteEvent += AbilityMenuEvent;
            abMenu.AddOption(option2, "3");

            var option3 = new MenuOption { OptionString = Abilities.Lizard.ToString() };
            option3.ExecuteEvent += AbilityMenuEvent;
            abMenu.AddOption(option3, "4");

            var option4 = new MenuOption { OptionString = Abilities.Spock.ToString() };
            option4.ExecuteEvent += AbilityMenuEvent;
            abMenu.AddOption(option4, "5");

            endGameMenu = new Menu(2)
            {
                ExtraText = "|||Round Finished|||",
                ExtraTextColor = Color.Red
            };

            var option5 = new MenuOption { OptionString = "New Game" };
            option5.ExecuteEvent += NewGameEvent;
            endGameMenu.AddOption(option5, CodeMessages.PLAYER_WAITING);

            var option6 = new MenuOption { OptionString = "Exit Game" };
            option6.ExecuteEvent += EndGameEvent;
            endGameMenu.AddOption(option6, CodeMessages.PLAYER_DISCONNECTED);

            mainMenu = new Menu(3)
            {
                ExtraText = "Name: ",
                ExtraTextColor = Color.BlueViolet
            };
            var option7 = new MenuOption() { OptionString = "Insert a Name" };
            var option8 = new MenuOption() { OptionString = "Exit Name" };
            var option9 = new MenuOption() { OptionString = "Start Game (Requires a Name)" };
            option7.ExecuteEvent += CreateNameMenu;
            option8.ExecuteEvent += EndGameEvent;
            option9.ExecuteEvent += OnStartGame;

            mainMenu.AddOption(option7, "name");
            mainMenu.AddOption(option9, "start");
            mainMenu.AddOption(option8, "quit");
        }

        private void OnStartGame(string read)
        {
            //
            if (ChosenName == null) { 
                Console.Clear();
                Colorful.Console.WriteLine("Please Choose a Name", Color.Aqua);
                return;
            }
            //Connect to The Server
            //
            //send your name to server
            showMainMenu = false;
            while (true)
            {
                if (controller.Connect(NetworkOptions.Ip, NetworkOptions.Port))
                {
                    Colorful.Console.WriteLine("Connected");
                    SendMessage(ChosenName);
                    break;
                }
                Console.WriteLine("Cant Connect");
            }
        }

        private void CreateNameMenu(string read)
        {
            //ChosenName = read;
            Colorful.Console.Write("Name-> ", Color.Aqua);
            ChosenName = Console.ReadLine();
            mainMenu.ExtraText += ChosenName;
            Console.Clear();
        }

        private void EndGameEvent(string read)
        {
            SendMessage(read);
            Console.Clear();
        }

        private void NewGameEvent(string read)
        {
            waitingResults = true;
            SendMessage(read);
        }

        private void AbilityMenuEvent(string read)
        {
            inPlay = false;
            Colorful.Console.WriteLine("Attack Sent");
            SendMessage(read);
        }

        private void ControllerOnMessageReceivedEvent(Message packet, string[] data)
        {
            ComputeMessage(Message.Deserialize(packet));
        }

        private void ComputeMessage(string[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                //We can show this Message to the user
                if (data[i].Contains(CodeMessages.PUBLIC_SERVER_MESSAGE.Message))
                {
                    Colorful.Console.WriteLine(data[0]);
                    for (int r = 1; r < data.Length; r++)
                    {
                        Colorful.Console.WriteLine($"{data[r]} ", Color.Aqua);
                    }
                    break;
                }

                //The server is Trying to initialize the game!
                if (data[i].Contains(CodeMessages.GAME_INITIALIZING.Message))
                {
                    Console.WriteLine("Game Changed To" + GameModes.GameInitializing);
                    clientGameState = GameModes.GameInitializing;
                    break;
                }

                //The server is Trying to Start the game!
                if (data[i].Contains(CodeMessages.GAME_STARTING.Message))
                {
                    Console.WriteLine("Game Changed To" + GameModes.GameStarted);
                    clientGameState = GameModes.GameStarted;
                    Console.Clear();
                    inPlay = true;
                    for (int j = 0; j < data.Length; j++)
                    {

                    }
                    break;
                }

                //The Server is Trying to End The Round
                if (data[i].Contains(CodeMessages.GAME_ROUND_ENDED.Message))
                {
                    Console.WriteLine("Game Changed To" + GameModes.GameEnded);
                    clientGameState = GameModes.RoundEnded;
                    waitingResults = false;
                    break;
                }
            }
        }

        public void Start()
        {
            while (!controller.shutDown)
            {
                UpdateClient();
            }
        }

        public void UpdateClient()
        {
            switch (clientGameState)
            {
                case GameModes.ConnectionOpen:
                    if (showMainMenu)
                    {
                        string option;
                        mainMenu.Show();
                        while (true)
                        {
                            option = mainMenu.ReadLine();
                            if (option != null)
                            {
                                break;
                            }
                            Colorful.Console.WriteLine("Option not available");
                        }
                        mainMenu.StartEvent(option);
                    }
                    break;
                case GameModes.ConnectionClosed:
                    break;
                case GameModes.GameInitializing:
                    break;
                case GameModes.GameStarted:
                    if (inPlay)
                    {
                        abMenu.Show();
                        string read;
                        while (true)
                        {
                            read = abMenu.ReadLine();
                            if (read != null)
                            {
                                break;
                            }
                            Colorful.Console.WriteLine("Option not available");
                        }
                        abMenu.StartEvent(read);
                    }
                    break;
                case GameModes.RoundEnded:
                    if (!waitingResults)
                    {
                        endGameMenu.Show();
                        string readEndMenu;
                        while (true)
                        {
                            readEndMenu = endGameMenu.ReadLine();
                            if (readEndMenu != null)
                            {
                                break;
                            }
                            Colorful.Console.WriteLine("Option not available");
                        }
                        endGameMenu.StartEvent(readEndMenu);
                    }
                    break;
                case GameModes.GameEnded:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Thread.Sleep(1000);
        }

        public void SendMessage(string message)
        {
            if (message == null)
            {
                Colorful.Console.WriteLine("Cannot Send empty messages");
                return;
            }
            controller.Send(message);
        }
    }
}
