#!/bin/bash

python word2fst.py $2 > test.txt
fstcompile --isymbols=syms.txt --osymbols=syms.txt  test.txt > test.fst
fstcompose test.fst $1.fst > result.fst
fstrmepsilon result.fst > result_red.fst
fstdraw    --isymbols=syms.txt --osymbols=syms.txt --portrait result_red.fst | dot -Tpdf  > result_red.pdf
