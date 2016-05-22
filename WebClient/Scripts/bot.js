/// <reference path="../../Scripts/jquery-1.6.4.js" />
/// <reference path="../../Scripts/jQuery.tmpl.js" />
/// <reference path="../../Scripts/jquery.cookie.js" />

$(function () {
    var game = $.connection.gameHub;

    game.client.refreshRoom = function (room) {

    };

    game.client.showRooms = function (rooms) {

    };

    game.client.addMessage = function (id, name, message) {

    };

    game.client.addUser = function (user, exists) {
        updateCookie();
    };

    game.client.addRoom = function (room, exists) {

    };

    game.client.changeUserName = function (oldUser, newUser) {
        if (newUser.Name === this.state.name) {
            updateCookie();
        }
    };

    game.client.leave = function (user) {

    };

    function updateCookie() {
        $.cookie('userid', game.state.id, { path: '/', expires: 30 });
    }

    $.connection.hub.url = "http://localhost:8080/signalr";
    $.connection.hub.logging = true;
    $.connection.hub.start({ transport: 'auto' }, function () {
        game.server.join()
            .done(function (success) {
                game.server.send('/nick AxiomBot')
                    .done(function () {
                        $('<span>Bot connected to server as AxiomBot</span><br/>').appendTo($('#messages'));
                        game.server.send('/join Bot')
                            .done(function () {
                                $('<span>Bot changed to room "Bot"</span><br/>').appendTo($('#messages'));
                        });
                    });
            });
    });

    function gameCreated() {

    }

    game.client.guessResult = function (guess, near, exact) {

    };

    game.client.endGame = function (winners) {

    };

});