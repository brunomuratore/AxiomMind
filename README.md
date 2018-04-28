# AxiomMind

### Table of contents

##### 1. Overview
##### 2. About the team
##### 3. How to setup/run
##### 4. Features available
##### 5. Technical details - Challenge One
##### 6. Technical details - Challenge Two
##### 7. Technical details - Challenge Three

### 1. Overview

This project is the Winner of VanHackathon 2017 for Axiom Zen challenge.
Called AxiomMind, it consists of a full multiplayer game of [MasterMind](https://en.wikipedia.org/wiki/Mastermind_(board_game)).
It provides a chat interface, a game interface, and a Bot that can help you win the game.
In our game, we have 8 positions and 8 colors on the game, and there is no round limit for players to guess the code.

### 2. About the team

The project was built by [Bruno Muratore](https://www.linkedin.com/in/brunomuratore) and [Felipe Sanchez](https://br.linkedin.com/in/sanchezit/en).

Challenges one and three (back-end and IA) were implemented by Bruno and challenge two (front-end) was implemented by Felipe.


### 3. How to setup/run

You can check our video explaining how to run and play the game here: https://youtu.be/6nQF20rv8Wk

AxiomMind was built using [Visual Studio 2015 Community](https://www.visualstudio.com/pt-br/products/visual-studio-community-vs.aspx), and this is the only requirement to run the project.

In order to run the project, open the solution in Visual Studio 2015, right click on 'Solution AxionMind' and go to properties.
Inside Common properties -> StartupProject, mark **'Multiple startup projects'** and set Action to **Start** for **both** projects.

Just press F5(run) and both a Console application (server) and a web browser (client) should start. You can execute no commands on the server, but you are ready to go on the client!

**Tip 1** Cookies are used to keep user's session. So in order to login with multiple users, you must **open a new incognito window (window, not tab) for each user**.

**Tip 2** The bot is also a client, and is not connected on server by default. To connect the bot, just open **bot.html** in a new incognito window.

**Tip 3** To play a game with the bot, join "Bot" room. Start a game, and now you can ask by hints typing **'/hint'** on the chat.


### 4. Features available

* User can choose its nick
* User can chat with other users that are in the same room
* User can change of room
* User can create a new room
* User can start a new game with everyone that is inside his room
* User can play MasterMind game alone
* User can play MasterMind game with other players (not only just two)
* User can play Mastermind with the Bot, asking for Hints.
* User can leave a game in the middle of it
* A game ends if no user is playing it anymore
* If a player disconnects in the middle of a game, other players in the game can continue playing without him
* User can use the chat or the UI for every action available.


### 5. Technical details - Challenge One

To build the multiplayer game MasterMind, we used C# within Visual Studio as our back-end server. The communication between server and client was done using [SignalR](http://www.asp.net/signalr). SignalR relies on WebSockets for its communication with the client, that can be written in any language that supports SignalR. 

The server is a [OWIN](http://owin.org/) self hosted application and runs as a console application.

All game logic is contained in the server. The user can request for the following actions:
* Connect to the server
* Change is nickname
* Join/Create a Room
* Start a game
* Make a guess
* Leave a game
* Ask for a hint
* Disconnect from the server

All validations are made on server, and the result is sent back to client.

### 6. Technical details - Challenge Two

The client is a web page, made with Html, CSS, jQuery and jQueryUI. It also contains references to SignalR JavaScript client in order to communicate with the server. The communication is done using webSockets, and if your browser don't support it, another protocol will be used instead.


### 7. Technical details - Challenge Three

AxiomBot connects itself as a client in the game. But the algorithm to calculate the hints run in the server.

Since Donald Knuth's classic algorithm would not be able to handle 8 positions with 8 colors, a genetic algorithm implementation was chosen.

It works by starting with an initial guess, and then based on previous results, mutations are applied on the previous guess, and for each mutation (2000 for each hint) it determines a score for it based on past results, and suggests the mutation with maximum score for the player.

Usually it takes 8\~20 (avg: 14) rounds for the bot correctly guess the code. It is good for finding 6\~7 exact matches, but it is not so good on finding the final code. The next improvement would be change the current algorithm for when we have enough good guesses (6 and 7 exact matches) to use a more naive solution instead of keep mutating.
