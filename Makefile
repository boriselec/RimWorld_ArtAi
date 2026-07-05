.PHONY: all build deploy test

all: build deploy

build:
	cd Source && dotnet build -c Release

deploy:
	cd Source && ./deploy

test:
	cd Source && ./test
