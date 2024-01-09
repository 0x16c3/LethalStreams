# Lethal Streams
A mod for your Twitch chat to integrate with your game.

## Features
- Donations:
  - Send signal transmissions to everyone.
- Bits:
  - Apply max sanity to a player.
  - Make player rotate 180 degrees.
  - Drain a player's stamina.
  - Drain a player's flashlight battery.
- Subscriptions:
  - Play air horn sound on a player's position.

You can determine the amount of bits required for each event in the config file.

## Usage
### Donations
If the viewer send a donation that has `say: <message>` in the message, the message will be sent to everyone in the game.
> Example: `nice stream but we need to get to the next round. say:come back`

### Bits
If the viewer includes any of the players' names in their bit message, the event will only affect that player.<br/>
If they don't include any names, the event will affect the streamer or if they're dead, the person that they are spectating.
> Example: `cheer1000 bye azumangadayilar flashlight`

### Subscriptions
If the viewer includes any of the players' names in their subscription message, the air horn sound will play on that player's position.

## Installation
1. Download the latest release from the releases page
2. Extract the zip file into BepInEx/plugins
3. Run the game once to generate the config file
4. Edit the config file to your liking
5. Run the game again to load the mod

## Configuration
The configuration file is located at BepInEx/config/lethalstreams.cfg<br/>
Make sure to set your Streamlabs Socket API token.
>⚠️ Set your Streamlabs token **ONLY** if you are the streamer. The other party members need to leave it blank.