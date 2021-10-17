# EnemyChanger

This mod allows one to dump and replace enemy textures.

## How to dump

Install the mod and start the game once, before closing it.  
Go to your save settings. There should be a file `EnemyChanger.GlobalSettings.json`. Open it with any text editor, like Notepad.exe on windows.  
There change the `"DumpSprites": false` to `"DumpSprites": true`. Save and close the file.  
Now when you open the game again it should freezeframe whenever there is an enemy spawning.  
It is recommended to turn this option to `false` again when you want to play with replaced textures.

## How to use custom textures

Install the mod and start the game once, before closing it.  
Go to your game files, where the mods are located.  
Next to the mod dll file, there should be a folder called `Sprites`.  
That folder will contain the image files you want the enemies to have in game.  
Those files will have cryptic names like `0A2CE4059196B94F3DFB284FD91CD1498F41314C0987403F0662A75D8552F62B1303A530AB1A4B108037BC951AB637E368126BB486B917C1A0DC45F97EEC0BC8.png` for the texture of a Vengefly.  
After putting textures there, just start the game again and see the textures for yourself.
