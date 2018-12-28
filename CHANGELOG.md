# Changelog
All notable changes to this project will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org/).

### ToDo's
- prevent starting of multiple instances
- add option to block/unblock internet access from tray
- add tweak restore mechanism
- make overvioew page usefull


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
