# -----------------------------------------------------------------------------------
#
# Licensed to the Apache Software Foundation (ASF) under one or more
# contributor license agreements.  See the NOTICE file distributed with
# this work for additional information regarding copyright ownership.
# The ASF licenses this file to You under the Apache License, Version 2.0
# (the ""License""); you may not use this file except in compliance with
# the License.  You may obtain a copy of the License at
# 
# http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an ""AS IS"" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#
# -----------------------------------------------------------------------------------

param (
	[Parameter(Mandatory=$false)]
	[int]
	$ServeDocs = 0,
	[Parameter(Mandatory=$false)]
	[int]
	$Clean = 1,
	# LogLevel can be: Diagnostic, Verbose, Info, Warning, Error
	[Parameter(Mandatory=$false)]
	[string]
	$LogLevel = 'Info'
)

$PSScriptFilePath = (Get-Item $MyInvocation.MyCommand.Path).FullName
$RepoRoot = (get-item $PSScriptFilePath).Directory.Parent.FullName;
$ApiDocsFolder = Join-Path -Path $RepoRoot -ChildPath "apidocs";
$ToolsFolder = Join-Path -Path $ApiDocsFolder -ChildPath "tools";
#ensure the /build/tools folder
New-Item $ToolsFolder -type directory -force
New-Item "$ToolsFolder\tmp" -type directory -force

# Go get docfx.exe if we don't have it
New-Item "$ToolsFolder\docfx" -type directory -force
$DocFxExe = "$ToolsFolder\docfx\docfx.exe"
if (-not (test-path $DocFxExe))
{
	Write-Host "Retrieving docfx..."
	$DocFxZip = "$ToolsFolder\tmp\docfx.zip"
	Invoke-WebRequest "https://github.com/dotnet/docfx/releases/download/v2.24/docfx.zip" -OutFile $DocFxZip
	#unzip
	Expand-Archive $DocFxZip -DestinationPath (Join-Path -Path $ToolsFolder -ChildPath "docfx")
}

# ensure we have NuGet
New-Item "$ToolsFolder\nuget" -type directory -force
$nuget = "$ToolsFolder\nuget\nuget.exe"
if (-not (test-path $nuget))
{
  Write-Host "Download NuGet..."
  Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nuget  
}

# ensure we have vswhere
New-Item "$ToolsFolder\vswhere" -type directory -force
 $vswhere = "$ToolsFolder\vswhere\vswhere.exe"
if (-not (test-path $vswhere))
{
   Write-Host "Download VsWhere..."
   $path = "$ToolsFolder\tmp"
   &$nuget install vswhere -OutputDirectory $path -Verbosity quiet
   $dir = ls "$path\vswhere.*" | sort -property Name -descending | select -first 1
   $file = ls -path "$dir" -name vswhere.exe -recurse
   mv "$dir\$file" $vswhere   
 }

 Remove-Item  -Recurse -Force "$ToolsFolder\tmp"

# delete anything that already exists
if ($Clean -eq 1) {
	Write-Host "Cleaning..."
	Remove-Item (Join-Path -Path $ApiDocsFolder "_site\*") -recurse -force -ErrorAction SilentlyContinue
	Remove-Item (Join-Path -Path $ApiDocsFolder "_site") -force -ErrorAction SilentlyContinue
	Remove-Item (Join-Path -Path $ApiDocsFolder "obj\*") -recurse -force -ErrorAction SilentlyContinue
	Remove-Item (Join-Path -Path $ApiDocsFolder "obj") -force -ErrorAction SilentlyContinue
}

# Build our custom docfx tools

$msbuild = &$vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
if ($msbuild) {
  Write-Host "MSBuild path = $msbuild";

  # Due to a bug with docfx and msbuild, we also need to set environment vars here
  # https://github.com/dotnet/docfx/issues/1969

  [Environment]::SetEnvironmentVariable("VSINSTALLDIR", "$msbuild")
  [Environment]::SetEnvironmentVariable("VisualStudioVersion", "15.0")

  # Then it turns out we also need 2015 build tools installed, wat!? 
  # https://www.microsoft.com/en-us/download/details.aspx?id=48159
  

  $msbuild = join-path $msbuild 'MSBuild\15.0\Bin\MSBuild.exe'
  if (-not (test-path $msbuild)) {
	throw "MSBuild not found!"
  }
  $sln = (Join-Path -Path $RepoRoot "src\docs\LuceneDocsPlugins\LuceneDocsPlugins.sln")
  & $nuget restore $sln
  $PluginsFolder = (Join-Path -Path $ApiDocsFolder "lucenetemplate\plugins")
  New-Item PluginsFolder -type directory -force
  & $msbuild $sln "/p:OutDir=$PluginsFolder"
}
else {
	throw "MSBuild not found!"
}

# NOTE: There's a ton of Lucene docs that we want to copy and re-format. I'm not sure if we can really automate this 
# in a great way since the docs seem to be in many places, for example:
# Home page - 	https://github.com/apache/lucene-solr/blob/branch_4x/lucene/site/xsl/index.xsl
# Wiki docs - 	https://wiki.apache.org/lucene-java/FrontPage?action=show&redirect=FrontPageEN - not sure where the source is for this
# Html pages - 	Example: https://github.com/apache/lucene-solr/blob/releases/lucene-solr/4.8.0/lucene/highlighter/src/java/org/apache/lucene/search/highlight/package.html - these seem to be throughout the source
#				For these ones, could we go fetch them and download all *.html files from Git?

$DocFxJson = Join-Path -Path $RepoRoot "apidocs\docfx.json"

Write-Host "Building metadata..."
if ($Clean -eq 1) {
	& $DocFxExe metadata $DocFxJson -l "obj\docfx.log" --loglevel $LogLevel --force
}
else {
	& $DocFxExe metadata $DocFxJson -l "obj\docfx.log" --loglevel $LogLevel
}
if($?) { 
	if ($ServeDocs -eq 0){
		# build the output		
		Write-Host "Building docs..."
		& $DocFxExe build $DocFxJson -l "obj\docfx.log" --loglevel $LogLevel
	}
	else {
		# build + serve (for testing)
		Write-Host "starting website..."
		& $DocFxExe $DocFxJson --serve
	}
}