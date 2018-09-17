# TorrentSwifter

## Description
This is just a toy project of mine for learning more about the BitTorrent protocol.
Basic support for leeching and seeding torrent files exists, but the project is nowhere close to being ready for consumers in the current state.

The project is developed using Visual Studio and is compatible with [.NET Standard 2.0](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) so it should work cross-platform, although it has only been tested on Windows.

I am also reserving rights to break backwards-compatibility if needed, until a more stable version is reached.

Also note that the CLI is extremely basic and has only been used for basic testing.

## TODO
### BEP
- [x] The BitTorrent Protocol ([BEP 3](http://www.bittorrent.org/beps/bep_0003.html))
- [x] UDP Tracker Protocol ([BEP 15](http://www.bittorrent.org/beps/bep_0015.html))
- [x] Multitracker Metadata Extension ([BEP 12](http://www.bittorrent.org/beps/bep_0012.html))
- [x] Tracker Returns Compact Peer Lists ([BEP 23](http://www.bittorrent.org/beps/bep_0023.html))
- [x] Tracker Protocol Extension: Scrape ([BEP 48](http://www.bittorrent.org/beps/bep_0048.html))
- [x] IPv6 Tracker Extension ([BEP 7](http://www.bittorrent.org/beps/bep_0007.html))
- [ ] Tracker Returns External IP ([BEP 24](http://www.bittorrent.org/beps/bep_0024.html))
- [x] Peer ID Conventions ([BEP 20](http://www.bittorrent.org/beps/bep_0020.html))
- [ ] WebSeed (GetRight style) ([BEP 19](http://www.bittorrent.org/beps/bep_0019.html))
- [ ] WebSeed (Hoffman-style) ([BEP 17](http://www.bittorrent.org/beps/bep_0017.html))
- [x] Local Service Discovery ([BEP 14](http://www.bittorrent.org/beps/bep_0014.html))
- [ ] Fast Extension ([BEP 6](http://www.bittorrent.org/beps/bep_0006.html))
- [ ] Extension for Peers to Send Metadata Files ([BEP 9](http://www.bittorrent.org/beps/bep_0009.html))
- [ ] Extension Protocol ([BEP 10](http://www.bittorrent.org/beps/bep_0010.html))
- [ ] Peer Exchange ([BEP 11](http://www.bittorrent.org/beps/bep_0011.html))
- [ ] DHT Protocol ([BEP 5](http://www.bittorrent.org/beps/bep_0005.html))
- [ ] Private Torrents ([BEP 27](http://www.bittorrent.org/beps/bep_0027.html))
- [ ] uTorrent transport protocol ([BEP 29](http://www.bittorrent.org/beps/bep_0029.html))
- [ ] Superseeding ([BEP 16](http://www.bittorrent.org/beps/bep_0016.html))
- [ ] Extension for partial seeds ([BEP 21](http://www.bittorrent.org/beps/bep_0021.html))
- [ ] Padding files and extended file attributes ([BEP 47](http://www.bittorrent.org/beps/bep_0047.html))
- [ ] Torrent Signing ([BEP 35](http://www.bittorrent.org/beps/bep_0035.html))

### Piece Selection
- [x] Random
- [x] Rarest first
- [x] Highly available first piece, then rarest first

### Modes
- [ ] Normal
- [ ] End-game
- [ ] Superseeding

### Other
- [ ] UPnP