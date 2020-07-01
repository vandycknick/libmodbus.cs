# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [v0.4.0]
### Fixed
- Fix error handling when connection closes, reject pending tasks
- Do not throw an error from the read loop on dispose

## [v0.3.0] - 2020-07-01
### Fixed
- Race condition were a request is parsed before a TaksCompletion is registered leading into a TimeoutException.

### Added
- Write multiple coils
- Added custom socket impl. that provides a wrapper around pipelines and is build more extensible.
- Client builder api

### Removed
- Client can only be instantiated via builder api
- Socket unofficial dependency

## [v0.2.0] - 2020-06-20
### Updated
- Improve connection instabilities

## [v0.1.0] - 2020-06-19
### Added
- Base TCP Modbus protocol implementation
- Implementation for ReadCoil request
- Implementation for WriteSingleCoil request
