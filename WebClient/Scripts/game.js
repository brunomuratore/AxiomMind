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

        $(".room").click(function () {
            game.server.send('/join ' + $(this)[0].id)
                .fail(function (e) {
                    addMessage(e, 'error');
                });
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

        var e = $('#new-user-template').tmpl(data).appendTo($('#users'));
                                       
        refreshUsers();

        if (!exists && this.state.name != user.Name) {
            addMessage(user.Name + ' just entered ' + this.state.room, 'notification');
            e.hide().fadeIn('slow');
        }

        updateCookie();
    };

    game.client.addRoom = function (room, exists) {
        var id = room.Name;

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
        gameselector.setCompleteSequence(hint);
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

    function changeNick()
    {
        var nickname = $("#msgNickname #name").val();
        if (nickname != null) {
            game.server.send("/nick " + nickname)
                .done(function () {
                    $("#msgNickname").dialog("close");
                    $("#msgStartGame").append("<p>Welcome " + nickname + "!</p>");
                    $("#msgStartGame").append("<p>You can play a game with all players in the room by clicking 'Start Game'.</p>");
                    $("#msgStartGame").append("<p>You can join a different room by clicking on it.</p>");
                    $("#msgStartGame").append("<p>Also, if you like, you can use the chat commands: '/join room', '/nick nick', '/start' and '/leavegame'.</p>");

                    $("#msgStartGame").dialog("open");
                })
                .fail(function (e) {
                    alert(e);
                    addMessage(e, 'error');
                });
        }
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
        game.server.start();
    });

    game.client.gameCreated = function() {
        $("#gameContainer").removeClass();
    }

    game.client.guessResult = function (guess, near, exact) {

        gameselector.setCompleteSequence(guess);
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
        $("#msgEndGame").html('');
        $("#msgEndGame").append("<p>WINNER: </p>");        
        for (var i = 0; i < winners.length; i++) {
            $("#msgEndGame").append("<p>" + winners[i] + "</p>");
        }
        $("#msgEndGame").dialog("open");
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
        setCompleteSequence(sequence) {
            var $colorsOptions = $(".colors").children();
            var $colorsHint = $(".line.active").children();
            for (var i = 0; i < 8 ; i++) {

                var hintColor = sequence.split("")[i];
                var color = $colorsOptions.eq(hintColor - 1).attr("class").split(" ")[0];
                gameselector.setColor(color);

                gameselector.setOption(i);
                gameselector.setSequenceColor();
                $colorsHint.eq(i).attr("class", gameselector.colorName);
            }
        }
    }

    // modal start
    var dialogOptions = {
        appendTo: "#game",
        dialogClass: "no-close",
        draggable: true,
        autoOpen: false,
        resizable: false,
        closeOnEscape: false,
        position: { my: "top", at: "top", of: "#game" }
    }

    $("#msgNickname").dialog(dialogOptions);
    $("#msgStartGame").dialog(dialogOptions);
    $("#msgEndGame").dialog(dialogOptions);

    $("#msgNickname").dialog({
        open: function () {
            $(this).off('submit').on('submit', function () {
                changeNick();
                $(this).dialog('close');
                return false;
            });
        },
        buttons: {
            "Confirm": function () {
                changeNick();
            }
        }
    });

    $("#msgNickname").dialog("open");

    $("#msgStartGame").dialog({
        buttons: {
            "Ok": function () {
                $("#msgStartGame").dialog("close");
            }
        }
    });

    $("#msgEndGame").dialog({
        buttons: {
            "Ok": function () {
                $("#msgEndGame").dialog("close");
                $("#gameContainer").addClass('hidden');
                $(".history").children().remove();
            }
        }
    });
    // modal end

    // interface events start
    var gameselector = new GameSelector();

    $("#btn-check").click(function () {
        //if submited ok
        game.server.sendGuess(gameselector.sequence.join(""))
            .done(function () {
                //wait message
            })
            .fail(function (e) {
                alert(e);
            });
    });
    // interface events end

    // drag&drop start

    var $colorPick = $("#gameContainer .colors"),
        $sequence = $("#gameContainer .line.active");

    $("div", $colorPick).draggable({
        cancel: "a.ui-icon",
        revert: "invalid",
        containment: "document",
        helper: "clone",
        cursor: "move"
    });
    $("div", $sequence).droppable({
        accept: "#gameContainer .colors div",
        activeClass: "none",
        drop: function (event, ui) {
            setColor(ui.draggable, $(this));
        }
    });
    function setColor($color, $position) {
        var color = $color.attr("class").split(" ")[0];
        gameselector.setColor(color);

        var index = $(".line.active div").index($position);
        gameselector.setOption(index);
        gameselector.setSequenceColor();
        $position.attr("class", gameselector.colorName);
    }

    $("#btnStartGame").button({
        icons: {
            primary: "ui-icon-power"
        }
    });

    // drag&drop end

    // end FrontEnd script

});