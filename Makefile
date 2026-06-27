.PHONY: all build deploy test

all: build deploy

build:
	cd Source && ./build

deploy:
	cd Source && ./deploy

test:
	cd Source && ./test
