all: sudoku-serial sudoku-omp sudoku-mpi

sudoku-serial: clean
	gcc -std=gnu11 -Wall -pedantic -g sudoku-serial.c -o sudoku-serial

sudoku-omp: clean
	gcc -std=gnu11 -Wall -pedantic -g -fopenmp sudoku-omp.c -o sudoku-omp

sudoku-mpi: clean
	mpicc -std=gnu11 -Wall -pedantic -g sudoku-mpi.c -o sudoku-mpi

clean:
	rm -f sudoku-serial
	rm -f sudoku-omp
	rm -f sudoku-mpi
	rm -f output/*.txt
