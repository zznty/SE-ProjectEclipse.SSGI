# SE-ProjectEclipse.SSGI

How to install:  
0. Clone the repo on your computer.
1. Go to the root directory (where the .sln file is), click on the address bar, type cmd and press enter.
2. Run the following command: mklink /J Bin64 \<insert game bin64 path in double quotes here\>  
for example: mklink /J Bin64 "C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Bin64"  
There should now be folder with a shortcut-looking icon called Bin64 in the root directory that leads to the game's bin64 folder where SpaceEngineers.exe is.
3. Open the solution and build solution. (Build > Build Solution)
4. Copy the resulting files in ProjectEclipse.SSGI/bin/Debug/net48 to Bin64/Plugins/Local/SSGI. Create the folder if it doesn't exist already.
5. Launch the game, search ProjectEclipse.SSGI in the plugin browser and enable it.
6. Restart the game when prompted.
