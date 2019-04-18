# CS:GO Rich Presence Service

Adds a Windows Services which passes RichPresence-data to Discord based on the CS:GO gamestate.
[Images](https://i.imgur.com/b8D7TpH.png)

## Getting Started

Download the Archive and execute 'install.bat' **AS ADMINISTRATOR**

### Prerequisites

A Windows-System. Unfortunately this Program is a Windows Service so no-can-do.  

## Known Issues  

* Sometimes Service does not detect running game. This is fixed be restarting the service. Probable cause: the port is not changed frequently enough.
* CS:GO needs to be installed in the default location

## Built With

* [Lilwiggy's Counter-Strike-RPC](https://github.com/Lilwiggy/counter-strike-rpc) - The basic idea
* [Lachee's Discord Richpresence C# Framework](https://github.com/Lachee/discord-rpc-csharp) - Dependency Management

## Contributing

This project as any of my projects will not be actively monitored.

## Versioning

Any Release will be a major Version (eg. V1, V2)

## Authors

* **C9Glax** - *Initial work*
Also credit to [Lilwiggy](https://github.com/Lilwiggy/counter-strike-rpc)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
