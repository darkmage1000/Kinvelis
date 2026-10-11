#!/bin/bash
# Publishes the player wiki: pushes the docs/ folder of the current commit to the gh-pages branch,
# which GitHub Pages serves at https://darkmage1000.github.io/Kinvelis/ (live about a minute later).
# Commit your changes to docs/ first (after Wildbond > Export Wiki Data, if the game's content changed).
cd "$(dirname "$0")/.." || exit 1
if [ -n "$(git status --porcelain docs)" ]; then echo "docs/ has uncommitted changes - commit them first."; exit 1; fi
git push origin "$(git subtree split --prefix docs HEAD)":refs/heads/gh-pages --force
