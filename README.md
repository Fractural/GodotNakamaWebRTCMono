# Godot Nakama WebRTC 🕸️

A C# version of [Snopek Game's Nakama WebRTC addon](https://gitlab.com/snopek-games/godot-nakama-webrtc)

Godot's [High-level Multiplayer API](https://docs.godotengine.org/en/stable/tutorials/networking/high_level_multiplayer.html)
can operate over WebRTC, however, it requires a _signaling server_ to establish
the WebRTC connections between all peers.

[Nakama](https://github.com/heroiclabs/nakama) is an Open Source, scalable game
server that provides many features, including user accounts, authentication,
matchmaking, chat, and [much more](https://heroiclabs.com/).

This Godot add-on provides some utility code to allow easily setting up those
connections using Nakama as the signaling server.

## Installation

1. Copy `addons/NakamaWebRTCMono`, `addons/com.heroiclabs.nakama`, `webrtc/` and `webrtc_debug/` directories into your project.

2. Download the Nakama client as a Nuget package into your C# project.

3. Add the `OnlineMatch.cs` singleton (in `addons/NakamaWebRTCMono`) as an [autoload in Godot](https://docs.godotengine.org/en/stable/getting_started/step_by_step/singletons_autoload.html).

## Demo and Template

This project is a full demo showing how to use this addon, and, in fact, makes
a pretty good template project to start from.

Download the full source code and import into Godot 3.5 to run.

Go into the NakamaServer directory and run  `docker-compose up -d` to start a
Nakama instance.

In both local and online mode, gamepads are supported, using the XBox A button
to attack. The keyboard controls are WASD for movement and SPACE for attack.

In local mode, you can control player 2 using the arrow keys and ENTER to
attack.

### Config

Please duplicate `config.template.yml` after cloning the repository and rename it to `config.yml`. The `config.yml` will let you specify credentials for using Twilio TURN servers. 

## Credits

* Snopek Game's Nakama WebRTC addon (MIT License): https://gitlab.com/snopek-games/godot-nakama-webrtc
* Official GDScript Nakama client (Apache License 2.0): https://github.com/heroiclabs/nakama-godot
* GDNative WebRTC plugin (MIT License): https://github.com/godotengine/webrtc-native

## License

Aside from the pieces listed under Credits above (which each have their own
licenses), everything else in this project is licensed under the MIT License.

