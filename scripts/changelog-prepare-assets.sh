#!/usr/bin/env bash

current_dir="$(dirname -- "$(readlink -f -- "$0";)";)"
assets_dir="$(dirname $current_dir)/docs/changelog-assets"

echo "assets dir: $assets_dir"

only_verify=0
failed_validation=0

if [[ "$1" == "verify" ]]; then
  echo "starting in validation mode"
  only_verify=1
fi

FILES="$assets_dir/*"

# checksums
for file in $FILES; do
  filename=$(basename -- "$file")
  if [[ "$filename" == "README.md" ]]; then
    continue
  fi

  extension="${filename##*.}"
  filename="${filename%.*}"

  echo "$file: hashing"
  filehash=$(cksum -a blake2b --untagged --length=128 $file | awk '{print $1}')

  if [[ "$filename" == "$filehash" ]]; then
    echo "$file: valid"
    continue
  fi

  echo "$file: FAILED"
  if [[ $only_verify -eq 1 ]]; then
    failed_validation=1
    continue
  fi

  fixed_file="$(dirname $file)/$filehash.$extension"
  echo "$file: moving to $fixed_file"

  mv "$file" "$fixed_file"
done

if [[ $failed_validation -eq 1 ]]; then
  echo "validation failed!"
  exit 1
else
  echo "validation was successful"
fi
