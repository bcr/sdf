all: build test

build: SDF.dll SDF.Print.dll

SDF.dll: SDF.cs SDF.Print.dll
	mcs -reference:NUnit.Framework -target:library SDF.cs

SDF.Print.dll: SDF.Print.cs
	mcs -reference:NUnit.Framework -reference:SDF.dll -target:library SDF.Print.cs

test: build
	mono ../../../OpenSource/NUnit-2.2.0-mono/bin/nunit-console.exe SDF.dll
