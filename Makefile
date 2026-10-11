.PHONY: all build deploy test

all: build deploy

build:
	cd Source && dotnet build -c Release

deploy:
	cd Source && ./deploy

# run the net472 tests on the newest installed .NET
test:
	dotnet build Source/Tests
	cd Source/Tests/bin/Debug/net472 && \
	echo '{"runtimeOptions":{"rollForward":"LatestMajor","framework":{"name":"Microsoft.NETCore.App","version":"0.0"}}}' > ArtAiTests.runtimeconfig.json && \
	dotnet ArtAiTests.exe
