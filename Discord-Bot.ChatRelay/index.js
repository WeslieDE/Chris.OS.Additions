const Discord = require('discord.js');
const axios = require('axios').default;

const {channelID, token, servers} = require('./config.json');
const client = new Discord.Client();

var playing = false;

client.login(token);

client.once('ready', () => 
{
    console.log("Coneccted to discord.");
});

client.on('message', message => {
    if(message.author.bot)
        return;

    if(message.channel.id == channelID)
    {
        servers.forEach(server => sendMessage(message.author.username, message.content, server));
    }
  });

function sendMessage(name, message, server) 
{ 
    const jdata = JSON.stringify({Name: name, Channel: 0, Message: message, Agent: true})

    axios({
        method: 'POST',
        url: server, 
        data: jdata, 
        headers:{'Content-Type': 'application/json; charset=utf-8'}
    })
} 


