.DEFAULT_GOAL 	:= default

ARTIFACTS 		:= $(shell pwd)/artifacts
BUILD			:= $(shell pwd)/.build
CONFIGURATION	:= Release
LIBRARY			:= src/LibModbus/LibModbus.csproj

.PHONY: default
default: restore package

.PHONY: restore
restore:
	dotnet restore

.PHONY: clean
clean:
	rm -rf $(BUILD)
	rm -rf $(ARTIFACTS)

.PHONY: run-example
run-example:
	dotnet run --project src/LibModbus.ConsoleApp

.PHONY: package
package:
	dotnet build $(LIBRARY) -c $(CONFIGURATION)
	dotnet pack $(LIBRARY) --configuration $(CONFIGURATION) \
		--no-build \
		--output $(ARTIFACTS) \
		--include-symbols
