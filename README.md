# AxiomMind

### Table of contents

##### 1. Overview
##### 2. About the team
##### 3. How to setup/run
##### 4. Features available
##### 5. Technical details - Challeng One
##### 6. Technical details - Challeng Two
##### 7. Technical details - Challeng Three


### 1. Overview

This project was built for Axiom Zen as the challenge we choose from VanHackathon challenge.
Called AxiomMind, it consinsts in a full multiplayer game of [MasterMind](https://en.wikipedia.org/wiki/Mastermind_(board_game)).
It provides a chat interface, the game interface, and a Bot that can help you win the game.


### 2. About the team

The project was built by [Bruno Muratore](https://www.linkedin.com/in/brunomuratore) and [Felipe Sanchez](https://br.linkedin.com/in/sanchezit/en).

Challenges one and three (back-end) were implemented by Bruno and challenge two (front-end) was implemented by Felipe.


### 3. How to setup/run

AxiomMind was built using [Visual Studio 2015 Community](https://www.visualstudio.com/pt-br/products/visual-studio-community-vs.aspx) and is the only requirement to run the project.

In order to run the project, open the solution in Visual Studio 2015, right click on 'Solution AxionMind' and go to properties.
Inside Common properties -> StartupProject, set the **'Multiple startup projects'** and set the Action to **Start** for **both** projects.

Just press F5 and both a Console application (server) and a web browser (client) should start. You can execute no commands on the server, but you are ready to go on the client!

**Tip 1** Cookies are used to keep user's session. So in order to login with multiple users, you can **open a new incognito window(window, not tab) for each user**.

**Tip 2** The bot is also a client, and is not connected on server by default. To connect the bot, just **open bot.html** in a new incognito window.

**Tip 3** To play a game with the bot, join "Bot" room. Start a game, and now you can ask by hints typing **'/hint'** on the chat.


### 4. Features available

* User can choose it's nick
* User can chat with other users in the same room
* User can change rooms
* User can create a new room
* User can start a new game with everyone that is inside his room
* User can play MasterMind game alone
* User can play MasterMind game with other players (not only just two)
* User can play Mastermind with the Bot, asking for Hints.
* User can leave a game in the middle of it
* A game ends if no user is playing it anymore
* If a player disconnects in the middle of a game, other players in the game can continue playing without him
* User can use the chat or the UI for every action available.


### 5. Technical details - Challeng One

To build the multiplayer game MasterMind, we used C# within Visual Studio as our back end server. The communication between server and client was done using [SignalR](http://www.asp.net/signalr). SignalR relies on WebSockets for its communication with the client, that can be written in any language that supports SignalR. 

The server is a [OWIN](http://owin.org/) self hosted application and runs as a console application.

All game logic is contained in the server. The user can request for the following actions:
* Connect to the server
* Change is nickname
* Join/Create a Room
* Start a game
* Leave a game
* Ask for a hint
* Disconnect from the server

All validation are made on server, and the result is sent back to client.

### 6. Technical details - Challeng Two

The client is a web page, made with Html, CSS, jQuery and jQueryUI. It also contains references to SignalR JavaScript client in order to communicate with the server. The communication will be done using webSockets, and if your browser don't support it, another protocol will be used instead.


### 7. Technical details - Challeng Three


AxiomBot connects itself as a client in the game. But the algorithm to calculate the hints run in the server.

The algorithm was created based on [Mike Gold's genetic algorithm](http://www.c-sharpcorner.com/article/mastermind-computer-player-using-genetic-algorithms-in-C-Sharp/).
His method was designed for 4 positions instead of 8, and has been ported to our server, modified and tweeked in order to handle our MasterMind game.

It works by starting with an initial guess, and then based on previous results, mutations are applied on the previous guess, and for each mutation (2000 for each hint) we determine a score for it based on past results, and suggests the mutation with maximum score for the player.
