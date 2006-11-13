all: build test

build: SDF.dll SDF.Print.dll SDF.Example.Console.exe

SDF.dll: SDF.cs SDFTokenString.cs
	mcs -debug -reference:NUnit.Framework -target:library SDF.cs SDFTokenString.cs

SDF.Print.dll: SDF.Print.cs SDF.dll
	mcs -debug -reference:NUnit.Framework -reference:SDF.dll -target:library SDF.Print.cs

SDF.Example.Console.exe: SDF.Example.Console.cs SDF.dll
	mcs -debug -reference:NUnit.Framework -reference:SDF.dll -target:exe SDF.Example.Console.cs

test: build
	mono --debug /Library/Frameworks/Mono.framework/Versions/1.2/lib/mono/1.0/nunit-console.exe SDF.dll
