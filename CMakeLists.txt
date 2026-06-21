cmake_minimum_required(VERSION 3.11)

project(PSXMoviePlayer)
enable_language(CSharp)

file(GLOB_RECURSE SOURCE_FILES ${PROJECT_SOURCE_DIR}/src/*.cs)

add_executable(PSXMoviePlayer ${SOURCE_FILES})

set_property(TARGET PSXMoviePlayer PROPERTY VS_DOTNET_REFERENCES
	"Microsoft.CSharp"
	"System"
	"System.Core"
	"System.Data"
	"System.Drawing"
	"System.Windows.Forms"
)

target_compile_options(PSXMoviePlayer PRIVATE "/unsafe")
