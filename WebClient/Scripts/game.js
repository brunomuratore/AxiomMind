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

    game.client.addMessageContent = function (id, content) {
        var e = $('#m-' + id).append(content);
        refreshMessages();
        updateUnread();
        e[0].scrollIntoView();
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
            name: user.Name,
            hash: user.Hash
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
        if(el)
            $('#' + id).replaceWith(newEl);
        else
            newEl.appendTo($('#roomsContainer'));
    };
    
    game.client.changeUserName = function (oldUser, newUser) {
        $('#u-' + oldUser.Name).replaceWith(
                $('#new-user-template').tmpl({
                    name: newUser.Name,
                    hash: newUser.Hash
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

    game.client.sendMeMessage = function (name, message) {
        addMessage('*' + name + '* ' + message, 'notification');
    };

    game.client.sendPrivateMessage = function (from, to, message) {
        addMessage('<emp>*' + from + '*</emp> ' + message, 'pm');
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
        game.server.start();
    });
});