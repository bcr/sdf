all: build test

build: SDF.dll SDF.Print.dll SDF.Example.Console.exe

SDF.dll: SDF.cs
	mcs -debug -reference:NUnit.Framework -target:library SDF.cs

SDF.Print.dll: SDF.Print.cs SDF.dll
	mcs -debug -reference:NUnit.Framework -reference:SDF.dll -target:library SDF.Print.cs

SDF.Example.Console.exe: SDF.Example.Console.cs SDF.dll
	mcs -debug -reference:NUnit.Framework -reference:SDF.dll -target:exe SDF.Example.Console.cs

test: build
	mono --debug ../../../OpenSource/NUnit-2.2.0-mono/bin/nunit-console.exe SDF.dll
