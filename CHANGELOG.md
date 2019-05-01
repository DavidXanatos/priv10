# Changelog
All notable changes to this project will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org/).

### ToDo's
- add tweak restore mechanism
- make overvioew page usefull
	- show a list of newly added programs to firewall
	- show recently blocked
	- show firewall status on/off/block
	- list of undone tweaks
- add prozess sniper feature (auto terminate selected prozesses)
- when cleaning up also remove obsolete rules

## [0.1h] - 2019-05-01

### Added
- Compatybility with windows 8.1 and server 2012
- first seen date for programs

### Fixed
- diagtrackrunner.exe and AeLookupSvc tweak are not longer offered to win10 users as on win 10 thay are not present
- crash when adding new category

## [0.1g] - 2019-01-06

### Added
- notification window has now path label clickable and opens explorer in the files directory
- issuing cleanup now also resets teh error flag

### Changed
- disabled autoresize for Name column
- open from tray trayicon now only react to left mouse button double click
- notification window direction stings are nor more clear
- allowed lan connections are now logged blue
- allowed multicast connections are now logged turquoise

### Fixed
- notification window next/previouse buttons now peoeprly enable/disable
- fixed crash when listing a not longer existing service
- fixed issue when resolving special adress ranges

## [0.1f] - 2019-01-03

### Added
- italian translation by garf02
- prevent starting of multiple instances inside the same user session
- add option to block/unblock internet access from tray

### Fixed
- not showing proepr "any protocol" string in lists
- rule violating actions are not colored yellow
- fixed ptiv10 failed to start when firewalls ervice was disabled

### Changed
- port and Ip columns doesn't autoresize anymore

## [0.1e] - 2018-12-29

### Fixed
- localization string screwup in firewall page
- englich fixed by spamtrash

### Added
- polish translation by spamtrash (programyzadarmo.net.pl)


## [0.1d] - 2018-12-28

### Added
- save last open page and open it on restart
- all setings can now enabled when running not as dmin, wne a admin only seting is to be chaged the cleint prompts fo a restart
- finish localizations upport

### Fixed
- fixed issue in retriving service by PID
- UAC bypass messing with -autorun argument

### Changed
- improved emabling and disabling of execution as service


## [0.1c] - 2018-12-27

### Added
- buttons in the firewallwindow get enabled/disabled based on list selection
- rule validation for the rule window

### Fixed
- not setting properly local port and address when creating a rule from the notification window
- fixed a bug in ip matching when a subnet was present
- rule window not saving position
- crash issue under windows 7 related to non existent app list

### Changed
- changes the way service names and PIDs are resolved


## [0.1b] - 2018-12-25

### Fixed
- possible crash when loading the list off installed uwp apps


## [0.1] - 2018-12-23

### Added
- Initial release
