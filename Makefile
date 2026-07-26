.POSIX:

APPIMAGE ?= OpenUtau-linux-x86_64.AppImage
DOTNET := dotnet
SOLUTION := OpenUtau.sln

.PHONY: all build test clean appimage install uninstall install-hooks

all: build

build:
	$(DOTNET) build $(SOLUTION) -c Release

test:
	$(DOTNET) test $(SOLUTION) -c Release --no-build

test-all:
	$(DOTNET) test $(SOLUTION) -c Release

clean:
	$(DOTNET) clean $(SOLUTION)
	rm -rf bin/

appimage: export APPIMAGE_EXTRACT_AND_RUN=1
appimage:
	$(DOTNET) publish OpenUtau -c Release -r linux-x64 --self-contained true -o bin/linux-x64/
	cp OpenUtau/Assets/AppRun bin/linux-x64/
	cp OpenUtau/Assets/OpenUtau.desktop bin/linux-x64/
	cp OpenUtau/Assets/logotype.png bin/linux-x64/
	linuxdeploy --appdir bin/linux-x64/ --output appimage
	mv OpenUtau-x86_64.AppImage OpenUtau-linux-x86_64.AppImage 2>/dev/null; true

appimage-arm64: export APPIMAGE_EXTRACT_AND_RUN=1
appimage-arm64:
	$(DOTNET) publish OpenUtau -c Release -r linux-arm64 --self-contained true -o bin/linux-arm64/
	cp OpenUtau/Assets/AppRun bin/linux-arm64/
	cp OpenUtau/Assets/OpenUtau.desktop bin/linux-arm64/
	cp OpenUtau/Assets/logotype.png bin/linux-arm64/
	linuxdeploy --appdir bin/linux-arm64/ --output appimage
	mv OpenUtau-aarch64.AppImage OpenUtau-linux-aarch64.AppImage 2>/dev/null; true

install: $(APPIMAGE) install.sh
	./install.sh $(APPIMAGE)

uninstall:
	rm -f "$(HOME)/.local/bin/OpenUtau"
	rm -f "$(HOME)/.local/share/applications/openutau.desktop"
	rm -f "$(HOME)/.local/share/icons/openutau.png"
	@echo "Removed OpenUtau from XDG paths."
	@echo "Data in \$$XDG_DATA_HOME/OpenUtau and cache in \$$XDG_CACHE_HOME/OpenUtau were NOT deleted."

install-hooks:
	cp scripts/pre-commit .git/hooks/pre-commit
	chmod +x .git/hooks/pre-commit
	@echo "Pre-commit hook installed: enforces tabs, K&R braces, LF line endings."
