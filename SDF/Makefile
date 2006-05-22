all: build test

build: SDF.dll SDF.Print.dll

SDF.dll: SDF.cs
	mcs -debug -reference:NUnit.Framework -target:library SDF.cs

SDF.Print.dll: SDF.Print.cs
	mcs -debug -reference:NUnit.Framework -reference:SDF.dll -target:library SDF.Print.cs

test: build
	mono --debug ../../../OpenSource/NUnit-2.2.0-mono/bin/nunit-console.exe SDF.dll
