/// <reference path="../../Scripts/jquery-1.6.4.js" />
/// <reference path="../../Scripts/jQuery.tmpl.js" />
/// <reference path="../../Scripts/jquery.cookie.js" />

$(function () {
    var game = $.connection.gameHub;

    function clearMessages() {
        $('#messages').html('');
    }

    function refreshMessages() { refreshList($('#messages')); }

    function clearUsers() {
        $('#users').html('');
        $('<div class="title">Users in your room</div><br />').appendTo($('#users'));
    }

    function refreshUsers() { refreshList($('#users')); }

    function refreshList(list) {
        if (list.is('.ui-listview')) {
            list.listview('refresh');
        }
    }

    function addMessage(content, type) {
        var e = $('<li/>').html(content).appendTo($('#messages'));
        refreshMessages();

        if (type) {
            e.addClass(type);
        }
        updateUnread();
        e[0].scrollIntoView();
        return e;
    }

    game.client.refreshRoom = function (room) {
        clearMessages();
        clearUsers();

        game.server.getUsers()
            .done(function (users) {
                $.each(users, function () {
                    game.client.addUser(this, true);
                });
                refreshUsers();

                $('#new-message').focus();
            });

        addMessage('Entered ' + room, 'notification');
    };

    game.client.showRooms = function (rooms) {
        $.each(rooms, function () {
            game.client.addRoom(this, true);
        });
    };

    game.client.addMessage = function (id, name, message) {
        var data = {
            name: name,
            message: message,
            id: id
        };

        var e = $('#new-message-template').tmpl(data)
                                          .appendTo($('#messages'));
        refreshMessages();
        updateUnread();
        e[0].scrollIntoView();
    };

    game.client.addUser = function (user, exists) {
        var id = 'u-' + user.Name;
        if (document.getElementById(id)) {
            return;
        }

        var data = {
            name: user.Name
        };

        var e = $('#new-user-template').tmpl(data)
                                       .appendTo($('#users'));
        refreshUsers();

        if (!exists && this.state.name != user.Name) {
            addMessage(user.Name + ' just entered ' + this.state.room, 'notification');
            e.hide().fadeIn('slow');
        }

        updateCookie();
    };

    game.client.addRoom = function (room, exists) {
        var id = 'u-' + room.Name;

        var data = {
            name: room.Name,
            count: room.Count
        };

        var newEl = $('#new-room-template').tmpl(data);
        var el = document.getElementById(id);
        if (el)
            $('#' + id).replaceWith(newEl);
        else
            newEl.appendTo($('#roomsContainer'));
    };

    game.client.changeUserName = function (oldUser, newUser) {
        $('#u-' + oldUser.Name).replaceWith(
                $('#new-user-template').tmpl({
                    name: newUser.Name
                })
        );
        refreshUsers();

        if (newUser.Name === this.state.name) {
            addMessage('Your name is now ' + newUser.Name, 'notification');
            updateCookie();
        }
        else {
            addMessage(oldUser.Name + '\'s nick has changed to ' + newUser.Name, 'notification');
        }
    };

    game.client.leave = function (user) {
        if (this.state.id != user.Id) {
            $('#u-' + user.Name).fadeOut('slow', function () {
                $(this).remove();
            });

            addMessage(user.Name + ' left ' + this.state.room, 'notification');
        }
    };

    $('#send-message').submit(function () {
        var command = $('#new-message').val();

        game.server.send(command)
            .fail(function (e) {
                addMessage(e, 'error');
            });

        $('#new-message').val('');
        $('#new-message').focus();

        return false;
    });

    game.client.hint = function (hint) {
        $('#new-message').val('/guess ' + hint);
    };

    $(window).blur(function () {
        game.state.focus = false;
    });

    $(window).focus(function () {
        game.state.focus = true;
        game.state.unread = 0;
        document.title = 'SignalR game';
    });

    function updateUnread() {
        if (!game.state.focus) {
            if (!game.state.unread) {
                game.state.unread = 0;
            }
            game.state.unread++;
        }
        updateTitle();
    }

    function updateTitle() {
        if (game.state.unread == 0) {
            document.title = 'SignalR game';
        }
        else {
            document.title = 'SignalR game (' + game.state.unread + ')';
        }
    }

    function updateCookie() {
        $.cookie('userid', game.state.id, { path: '/', expires: 30 });
    }

    addMessage('Welcome to AxiomMind.', 'notification');

    $('#new-message').val('');
    $('#new-message').focus();

    $.connection.hub.url = "http://localhost:8080/signalr";
    $.connection.hub.logging = true;
    $.connection.hub.start({ transport: 'auto' }, function () {
        game.server.join()
            .done(function (success) {
                if (success === false) {
                    $.cookie('userid', '');
                    addMessage('To start, choose a name typing "/nick nickname".', 'notification');
                }
            });
    });

    $("#btnStartGame").click(function () {
        game.server.start()
            .done(function (success) {
                if (success === true) {
                    gameCreated();
                }
            });
    });

    function gameCreated() {
        //todo: Implement here
        //display game UI for empty game
    }

    game.client.guessResult = function (guess, near, exact) {
        //TODO: Show results of guess
        //guess = string
        //near = int, number of near tags
        //exact = int, number of exact tags
        $(".history").prepend($(".hidden .line").clone());

        for (var i = 0; i < gameselector.sequence.length; i++) {
            $(".history .line")
              .children().eq(i)
              .attr(
                "class",
                gameselector.colors[gameselector.sequence[i]]
              );
        }
        for (var i = 0; i < exact; i++) {
            $(".history .line .result")
              .children().eq(i)
              .attr(
                "class",
                "black border"
              );
        }
        for (var i = exact; i < near + exact; i++) {
            $(".history .line .result")
                .children().eq(i)
                .attr(
                "class",
                "white border"
                );
        }

        $(".line.active div").attr("class", "white");
        gameselector.reset();
    };

    game.client.endGame = function (winners) {
        //TODO: End game - Show winners
        //winners = array of strings
    };

    // start FrontEnd script
    class GameSelector {
        constructor() {
            this.colors = ["white", "red", "orange", "yellow", "green", "lightblue", "blue", "purple", "pink"];
            this.sequence = [0, 0, 0, 0, 0, 0, 0, 0];
            this.colorName = "none";
            this.colorIndex = 0;
            this.option = 0;
            this.sequenceList = [];
        }
        setOption(index) {
            this.option = index;
        }
        setColor(color) {
            this.colorName = color;
            this.colorIndex = this.colors.indexOf(color);
        }
        setSequenceColor() {
            this.sequence[this.option] = this.colorIndex;
        }
        reset() {
            this.sequence = [0, 0, 0, 0, 0, 0, 0, 0];
            this.colorName = "none";
            this.colorIndex = 0;
            this.option = 0;
            this.sequenceList = [];
        }
    }
    var gameselector = new GameSelector();

    $(".colors div").click(function () {
        var color = $(this).attr("class");
        gameselector.setColor(color);
    });

    $(".line.active div").click(function () {
        var index = $(".line.active div").index(this);
        gameselector.setOption(index);
        gameselector.setSequenceColor();
        $(this).attr("class", gameselector.colorName);
    });

    $("#btn-check").click(function () {
        //if submited ok
        game.server.sendGuess(gameselector.sequence.join(""))
            .done(function (success) {
                if (success === false) {
                    //error message
                }
            });
    });

    // end FrontEnd script

});