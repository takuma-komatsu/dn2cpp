cmake_minimum_required(VERSION 3.20)
project(default_culture_locale LANGUAGES C)
add_library(default_culture_locale SHARED locale.c)
target_link_libraries(default_culture_locale PRIVATE ${CMAKE_DL_LIBS})
