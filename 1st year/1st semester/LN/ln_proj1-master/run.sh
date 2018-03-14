#!/bin/bash
#Transdutores auxiliares que consomem as letras do alfabeto, algarismo e caracteres "_"
fstcompile --isymbols=syms.txt --osymbols=syms.txt  abcd.txt > abcd.fst
fstcompile --isymbols=syms.txt --osymbols=syms.txt  digits.txt > digits.fst
fstcompile --isymbols=syms.txt --osymbols=syms.txt  underscore.txt > underscore.fst

####Transdutor Romano######
fstcompile --isymbols=syms.txt --osymbols=syms.txt  transdutorRomanos.txt > transdutorRomanos.fst
fstdraw    --isymbols=syms.txt --osymbols=syms.txt --portrait transdutorRomanos.fst | dot -Tpdf  > transdutorRomanos.pdf

####Transdutor 1#####
fstunion transdutorRomanos.fst abcd.fst > roman_abcd.fst
fstconcat roman_abcd.fst underscore.fst > roman_abcd_score.fst
fstclosure roman_abcd_score.fst | fstarcsort > transdutor1.fst
fstdraw --isymbols=syms.txt --osymbols=syms.txt --portrait transdutor1.fst | dot -Tpdf  > transdutor1.pdf

####Transdutor 2 - primeira codificacao#####
fstcompile --isymbols=syms.txt --osymbols=syms.txt  codif.txt > codif.fst
fstunion codif.fst abcd.fst > codif_abcd.fst
fstconcat codif_abcd.fst underscore.fst > codif_abcd_score.fst
fstclosure codif_abcd_score.fst | fstarcsort > transdutor2.fst
fstdraw --isymbols=syms.txt --osymbols=syms.txt --portrait transdutor2.fst | dot -Tpdf  > transdutor2.pdf

####Transdutor 3 - segunda codificacao######
fstcompile --isymbols=syms.txt --osymbols=syms.txt  codif2.txt > codif2.fst
fstunion codif2.fst digits.fst > codif2_digits.fst
fstconcat codif2_digits.fst underscore.fst > codif2_digits_score.fst
fstclosure codif2_digits_score.fst | fstarcsort > transdutor3.fst
fstdraw --isymbols=syms.txt --osymbols=syms.txt --portrait transdutor3.fst | dot -Tpdf  > transdutor3.pdf

####Transdutor codificador#####
fstcompose transdutor1.fst transdutor2.fst | fstarcsort > trans_1_2.fst
fstcompose trans_1_2.fst transdutor3.fst | fstarcsort > codificador.fst
fstdraw --isymbols=syms.txt --osymbols=syms.txt --portrait codificador.fst | dot -Tpdf  > codificador.pdf

####Transdutor descodificador####
fstinvert codificador.fst > descodificador.fst
fstdraw --isymbols=syms.txt --osymbols=syms.txt --portrait descodificador.fst | dot -Tpdf  > descodificador.pdf

####Codifica o email#######
fstcompile --isymbols=syms.txt --osymbols=syms.txt  email1_codificar.txt > email1_codificar.fst

#Codifica por etapas
fstcompose email1_codificar.fst transdutor1.fst > email1_trans1_codificado.fst
fstrmepsilon email1_trans1_codificado.fst > email1_trans1_codificado_reduzido.fst
fstdraw --isymbols=syms.txt --osymbols=syms.txt --portrait email1_trans1_codificado_reduzido.fst | dot -Tpdf  > email1_trans1_codificado.pdf

fstcompose email1_trans1_codificado.fst transdutor2.fst > email1_trans2_codificado.fst
fstrmepsilon email1_trans2_codificado.fst > email1_trans2_codificado_reduzido.fst
fstdraw --isymbols=syms.txt --osymbols=syms.txt --portrait email1_trans2_codificado_reduzido.fst | dot -Tpdf  > email1_trans2_codificado.pdf

fstcompose email1_trans2_codificado.fst transdutor3.fst > email1_trans3_codificado.fst
fstrmepsilon email1_trans3_codificado.fst > email1_trans3_codificado_reduzido.fst
fstdraw --isymbols=syms.txt --osymbols=syms.txt --portrait email1_trans3_codificado_reduzido.fst | dot -Tpdf  > email1_trans3_codificado.pdf

#Codifica pelo transdutor Codificador e reduzir
fstcompose email1_codificar.fst codificador.fst > email1_codificado.fst
fstrmepsilon email1_codificado.fst > email1_codificado_reduzido.fst
fstdraw --isymbols=syms.txt --osymbols=syms.txt --portrait email1_codificado_reduzido.fst | dot -Tpdf  > email1_codificado.pdf

 ####Descodifica os emais#####
#Inverte os Transdutores, passo necessario para mostrar as etapas de descodificacao
fstinvert transdutor1.fst > transdutor1_invertido.fst
fstinvert transdutor2.fst > transdutor2_invertido.fst
fstinvert transdutor3.fst > transdutor3_invertido.fst

#Descodifica o primeiro email recebido
fstcompile --isymbols=syms.txt --osymbols=syms.txt  email1_descodificar.txt > email1_descodificar.fst

#Descodifica por etapas - email 1
fstcompose email1_descodificar.fst transdutor3_invertido.fst > email1_trans3_descodificado.fst
fstrmepsilon email1_trans3_descodificado.fst > email1_trans3_descodificado_reduzido.fst
fstdraw --isymbols=syms.txt --osymbols=syms.txt --portrait email1_trans3_descodificado_reduzido.fst | dot -Tpdf  > email1_trans3_descodificado.pdf

fstcompose email1_trans3_descodificado.fst transdutor2_invertido.fst > email1_trans2_descodificado.fst
fstrmepsilon email1_trans2_descodificado.fst > email1_trans2_descodificado_reduzido.fst
fstdraw --isymbols=syms.txt --osymbols=syms.txt --portrait email1_trans2_descodificado_reduzido.fst | dot -Tpdf  > email1_trans2_descodificado.pdf

fstcompose email1_trans2_descodificado.fst transdutor1_invertido.fst > email1_trans1_descodificado.fst
fstrmepsilon email1_trans1_descodificado.fst > email1_trans1_descodificado_reduzido.fst
fstdraw --isymbols=syms.txt --osymbols=syms.txt --portrait email1_trans1_descodificado_reduzido.fst | dot -Tpdf  > email1_trans1_descodificado.pdf

#Descodifica pelo transdutor descodificador e reduz - email 1
fstcompose email1_descodificar.fst descodificador.fst > email1_descodificado.fst
fstrmepsilon email1_descodificado.fst > email1_descodificado_reduzido.fst
fstdraw --isymbols=syms.txt --osymbols=syms.txt --portrait email1_descodificado_reduzido.fst | dot -Tpdf  > email1_descodificado.pdf

#Descodifica o segundo email recebido
fstcompile --isymbols=syms.txt --osymbols=syms.txt  email2_descodificar.txt > email2_descodificar.fst

#Descodifica por etapas - email 2
fstcompose email2_descodificar.fst transdutor3_invertido.fst > email2_trans3_descodificado.fst
fstrmepsilon email2_trans3_descodificado.fst > email2_trans3_descodificado_reduzido.fst
fstdraw --isymbols=syms.txt --osymbols=syms.txt --portrait email2_trans3_descodificado_reduzido.fst | dot -Tpdf  > email2_trans3_descodificado.pdf

fstcompose email2_trans3_descodificado.fst transdutor2_invertido.fst > email2_trans2_descodificado.fst
fstrmepsilon email2_trans2_descodificado.fst > email2_trans2_descodificado_reduzido.fst
fstdraw --isymbols=syms.txt --osymbols=syms.txt --portrait email2_trans2_descodificado_reduzido.fst | dot -Tpdf  > email2_trans2_descodificado.pdf

fstcompose email2_trans2_descodificado.fst transdutor1_invertido.fst > email2_trans1_descodificado.fst
fstrmepsilon email2_trans1_descodificado.fst > email2_trans1_descodificado_reduzido.fst
fstdraw --isymbols=syms.txt --osymbols=syms.txt --portrait email2_trans1_descodificado_reduzido.fst | dot -Tpdf  > email2_trans1_descodificado.pdf

#Descodifica pelo transdutor descodificador e reduz - email 2
fstcompose email2_descodificar.fst descodificador.fst > email2_descodificado.fst
fstrmepsilon email2_descodificado.fst > email2_descodificado_reduzido.fst
fstdraw    --isymbols=syms.txt --osymbols=syms.txt --portrait email2_descodificado_reduzido.fst | dot -Tpdf  > email2_descodificado.pdf
