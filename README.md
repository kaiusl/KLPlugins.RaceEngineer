# Race Engineer Plugin - SimHub race strategy helper

# NOTES
- ** EVERYTHING IS SUBJECT TO CHANGE **
- Plugin needs to read a setup file to get brake ducts. Currently it assumes that the current setup is names 'current.json'
- Assumes that setups are in 'C:\Users\\&lt;user name&gt;\Documents\Assetto Corsa Competizione\Setups'

# Features
- Collect data to provide some predictions for the session taking into account given weather conditions.
  - ideal tyre pressures
  - needed fuel at session start (min, max, avg)
- Calculate statistics of tyre/brake temps and pressures to aid with tyre pressure and brake duct changes.
- Lap and fuel use history and give some statistics on it.
- Using above two calculate needed fuel in race, remaining laps on fuel and in session. Gives min, max, avg based of fuel and lap history of those for you to decide if you want to be safe or take a risk.
- Provide precise weather report.
- Color calculator with 4 available colors, instead of 3 in SimHub's DashStudio. I use it to set ideal range as one color. Color interpolation is done in HSV to provide more natural color transitions. This is available for tyre pres/temp (current and stats), brake temp (current and stats).
