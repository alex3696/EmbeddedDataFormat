cmake_minimum_required(VERSION 3.20.0)
set(project_name edf)
project(${project_name} VERSION 0.1.0 LANGUAGES C)

add_compile_options(-Wno-write-strings)
add_compile_options(-Wno-incompatible-pointer-types)

include_directories(
    src/common/
    src/converter/
)

file(GLOB project
    src/common/*
    src/converter/*
)

#перенос exe из папки билд в корень
SET(EXECUTABLE_OUTPUT_PATH ${PROJECT_BINARY_DIR}/../)

add_executable(${project_name} ${project})

