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

*
