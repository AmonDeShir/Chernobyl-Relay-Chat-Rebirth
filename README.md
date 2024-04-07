# DISCLAIMER  
Original mod by TKGP. Please, hit me up if you are against me trying to revive this project and want me to shut it down!

# Chernobyl Relay Chat Rebirth
An IRC-based chat app for Anomaly, originally developed by TKGP for CoC. Features an independent client as well as in-game chat, automatic death messages, and compatibility with all other addons.

# Official CRCR Discord Server
[Join](https://discord.gg/crcr) to get help, leave feedback or just to hang out! 

# Installation
1. Install the [.NET framework](https://www.microsoft.com/net/download/framework) if you don't have it already  
2. Extract the contents of the CRCR.zip wherever you like
3. Copy the included gamedata folder to your game directory
4. (Optional) Install the [Anomaly Mod Configuration Menu](https://www.moddb.com/mods/stalker-anomaly/addons/anomaly-mod-configuration-menu) for advanve mod configuration

# Compatibility with non-US keyboard layouts
Stalker reads the key position on the keyboard ([DIK](https://community.bistudio.com/wiki/DIK_KeyCodes)) rather than its value after being pressed. For example, when using AZERTY, you don't have to manually change W to Z; you simply press Z, and the game interprets it as W. However, a problem arises when typing in the chat. You need to specifically remember which character on your AZERTY keyboard corresponds to the QWERTY character.

This is not a problem anymore; install [MCM](https://www.moddb.com/mods/stalker-anomaly/addons/anomaly-mod-configuration-menu) and change the keyboard layout to one of the many available (currently 55). 

I don't promise that everything will work perfectly (Stalker in the English version doesn't support many characters, such as £, ń, ł, Æ, ř, etc.). Additionally, different languages have varying approaches to entering diacritical marks. Sometimes it's `AltGr + character`, sometimes `AltGr + number`, and the combinations can be different and sometimes quite complex ([look up how to type Czech characters with a háček XD](https://www.czechtime.cz/article/how-to-type-czech-characters-on-keyboard/)). I don't have the energy to implement this for all 55 supported languages, especially considering that most of the obtained characters are not supported in Stalker.

I tried to adjust the keys as best as I could. However, if you think that any key could be assigned better or if there's a particular character missing, please create an issue or let me know on Discord (amon_de_shir). The layouts were generated by a script, so there's definitely room for improvement.

# Usage
Run Chernobyl Relay Chat Rebirth.exe; the application must be running for in-game chat to work.  
After connecting, click the Options button to change your name and other settings, then launch Anomaly.  
Once playing, press Enter (by default) to bring up the chat interface and Enter again to send your message, or Escape to close without sending.  
You may use text commands from the game or client by starting with a /. Use /commands to see all available commands.  

# What's Planned  
I AM CURRENTLY LOOKING FOR ANYONE WHO IS WILLING TO HELP WITH DEVELOPING THIS MOD!  
IF ANYONE IS WILLING TO HELP WITH ANY OF THIS FEATURES, FEEL FREE TO HIT ME ON DISCORD: anchorpoint#6144
- [ ] Advanced anti-spam and moderation features
- [ ] New interface
- [ ] (possibly) Own private IRC-server
- [x] New In-game chat GUI and (possibly) integrating it in Anomaly's 3D PDA
- [ ] G.A.M.M.A modpack compatibility

# Credits
TKGP: Original CRC  
EveNaari: Huge help with C#, Microsoft Visual Studio  
GitHub: Octokit  
Max Hauser: semver  
Mirco Bauer: SmartIrc4Net  
nixx quality: GitHubUpdate  
  
av661194, OWL, XMODER, Anchorpoint: Russian translation  
Rebirth changes: Anchorpoint  
PDA Integration AmonDeShir
Compatibility with non-US keyboard layouts: AmonDeShir
