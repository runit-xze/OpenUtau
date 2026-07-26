.POSIX:

DOTNET := dotnet

.PHONY: all build test clean build-core build-plugin build-app build-test
.PHONY: appimage appimage-arm64 install uninstall install-hooks

all: build

build: build-core build-plugin build-app

build-core:
	$(DOTNET) build src/OpenUtau.Core/OpenUtau.Core.csproj -c Release

build-plugin:
	$(DOTNET) build src/OpenUtau.Plugin.Builtin/OpenUtau.Plugin.Builtin.csproj -c Release

build-app:
	$(DOTNET) build src/OpenUtau/OpenUtau.csproj -c Release

build-test:
	$(DOTNET) build tests/OpenUtau.Test/OpenUtau.Test.csproj -c Release

test:
	$(DOTNET) test tests/OpenUtau.Test/OpenUtau.Test.csproj -c Release -nobuild

test-all:
	$(DOTNET) test tests/OpenUtau.Test/OpenUtau.Test.csproj -c Release

clean:
	$(DOTNET) clean src/OpenUtau.Core/OpenUtau.Core.csproj
	$(DOTNET) clean src/OpenUtau.Plugin.Builtin/OpenUtau.Plugin.Builtin.csproj
	$(DOTNET) clean src/OpenUtau/OpenUtau.csproj
	$(DOTNET) clean tests/OpenUtau.Test/OpenUtau.Test.csproj
	rm -rf bin/

appimage:
	$(DOTNET) publish src/OpenUtau/OpenUtau.csproj -c Release -r linux-x64 --self-contained true -o bin/linux-x64/
	cp src/OpenUtau/Assets/AppRun bin/linux-x64/
	cp src/OpenUtau/Assets/OpenUtau.desktop bin/linux-x64/
	cp src/OpenUtau/Assets/logotype.png bin/linux-x64/
	APPIMAGE_EXTRACT_AND_RUN=1 linuxdeploy --appdir bin/linux-x64/ --output appimage
	mv OpenUtau-x86_64.AppImage OpenUtau-linux-x86_64.AppImage 2>/dev/null || true

appimage-arm64:
	$(DOTNET) publish src/OpenUtau/OpenUtau.csproj -c Release -r linux-arm64 --self-contained true -o bin/linux-arm64/
	cp src/OpenUtau/Assets/AppRun bin/linux-arm64/
	cp src/OpenUtau/Assets/OpenUtau.desktop bin/linux-arm64/
	cp src/OpenUtau/Assets/logotype.png bin/linux-arm64/
	APPIMAGE_EXTRACT_AND_RUN=1 linuxdeploy --appdir bin/linux-arm64/ --output appimage
	mv OpenUtau-aarch64.AppImage OpenUtau-linux-aarch64.AppImage 2>/dev/null || true

install:
	./install.sh

uninstall:
	rm -f "$(HOME)/.local/bin/OpenUtau"
	rm -f "$(HOME)/.local/share/applications/openutau.desktop"
	rm -f "$(HOME)/.local/share/icons/openutau.png"
	@echo "Removed OpenUtau from XDG paths."

install-hooks:
	cp scripts/pre-commit .git/hooks/pre-commit
	chmod +x .git/hooks/pre-commit
	@echo "Pre-commit hook installed: enforces tabs, K&R braces, LF line endings."
