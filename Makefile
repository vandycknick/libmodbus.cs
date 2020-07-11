.DEFAULT_GOAL 	:= default

ARTIFACTS 		:= $(shell pwd)/artifacts
BUILD			:= $(shell pwd)/.build
CONFIGURATION	:= Release
LIBRARY			:= src/LibModbus/LibModbus.csproj

.PHONY: default
default: setup package

.PHONY: setup
setup:
	dotnet restore

.PHONY: clean
clean:
	rm -rf $(BUILD)
	rm -rf $(ARTIFACTS)

.PHONY: run-example
run-example:
	dotnet run --project src/LibModbus.ConsoleApp

.PHONY: test
test: 
	dotnet test

.PHONY: test-server
test-server:
	cd test/ModbusTestServer && \
		pipenv run python main.py

.PHONY: package
package:
	dotnet build $(LIBRARY) -c $(CONFIGURATION)
	dotnet pack $(LIBRARY) --configuration $(CONFIGURATION) \
		--no-build \
		--output $(ARTIFACTS) \
		--include-symbols
