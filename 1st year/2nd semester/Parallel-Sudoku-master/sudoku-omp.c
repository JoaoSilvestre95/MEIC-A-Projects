#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <omp.h>

#define MAXARGS 2

int thread_counter;

typedef struct cell {
  unsigned int value;         //Value for that cell
  unsigned int * candidates;  //Array of candidates for that cell
} CELL;

void print_board(unsigned int size, CELL **board) {
  for (unsigned int i = 0; i < size; i++) {
    for (unsigned int j = 0; j < size; j++) {
      printf("%d", board[i][j].value);
      if ((j + 1) == size)
        printf("\n");
      else
        printf(" ");
    }
  }
}

CELL** copy_board(unsigned int size, CELL **board){
  CELL **copy_matrix  = (CELL **) malloc(size * sizeof(CELL *));
  for (unsigned int i = 0; i < size; i++) {
    copy_matrix[i] = (CELL *) malloc(size * sizeof(CELL));
  }
  for(unsigned int i = 0; i < size; i++) {
    for(unsigned int j = 0; j < size; j++) {
      copy_matrix[i][j].value = board[i][j].value;
      copy_matrix[i][j].candidates = (unsigned int *) malloc(size * sizeof(unsigned int));
      for(unsigned int k = 0; k < size; k++) {
        copy_matrix[i][j].candidates[k] = board[i][j].candidates[k];
      }
    }
  }
  return copy_matrix;
}

void free_board(CELL **board,unsigned int size){
  for(unsigned int i = 0; i < size; i++) {
    for(unsigned int j = 0; j < size; j++) {
      free(board[i][j].candidates);
    }
    free(board[i]);
  }
  free(board);
}

CELL** readFromFile(char *arg, unsigned int *l){
  CELL **matrix = NULL;
  FILE *file;
  char *line = NULL;
  size_t len = 0;
  ssize_t read;
  unsigned int x = 0;
  unsigned int size;


  if ((file = fopen(arg, "r")) == NULL) {
    fputs("Error! Opening file\n", stderr);
    exit(EXIT_FAILURE);
  }

  if ((read = getline(&line, &len, file)) != -1) {
    *l = atoi(line);
    if (*l < 2 || *l > 9) {
      fputs("Bad l value!\n", stderr);
      exit(EXIT_FAILURE);
    }
    size = (*l) * (*l);

    matrix = (CELL **) malloc(size* sizeof(CELL *));
    for (unsigned int i = 0; i < size; i++) {
      matrix[i] = (CELL *) malloc(size * sizeof(CELL));
      for (unsigned int j = 0; j < size; j++) {
        matrix[i][j].candidates = (unsigned int *) malloc(size * sizeof(unsigned int));
        for (unsigned int k = 0; k < size; k++) {
          matrix[i][j].candidates[k]=1;
        }
      }
    }
  }

  while ((read = getline(&line, &len, file)) != -1) {
    unsigned int column = 0;
    char *token;
    token = strtok(line, " \n");
    while (token != NULL) {
      matrix[x][column].value = atoi(token);
      column++;
      token = strtok(NULL, " \n");
    }
    x++;
  }
  free(line);
  fclose(file);
  return matrix;
}

void eliminateCandidatesFromRowAndCol(unsigned int x, unsigned int y, unsigned int size, CELL** matrix)
{
  for(unsigned int i = 0; i < size; i++) {
    if(matrix[i][y].value != 0) //check all col
      matrix[x][y].candidates[matrix[i][y].value - 1] = 0;
    if(matrix[x][i].value != 0) //check all row
      matrix[x][y].candidates[matrix[x][i].value - 1] = 0;
  }
}

void eliminateCandidatesFromSquare(unsigned int x, unsigned int y, unsigned int l, CELL** matrix)
{
  unsigned int start_row = (x/l) * l; //start row at small square
  unsigned int start_col = (y/l) * l; //start col at small square
  for(unsigned int row = 0; row < l; row++)
    for(unsigned int col = 0; col < l; col++)
      if(matrix[start_row + row][start_col + col].value) //check n by n square
        matrix[x][y].candidates[matrix[start_row + row][start_col + col].value - 1] = 0;
}

void newCandidates(CELL ** matrix, unsigned int size, unsigned int x, unsigned int y){
  for (unsigned int k = 0; k < size; k++)
    matrix[x][y].candidates[k]=1;
}

void resetCandidates(CELL ** matrix, unsigned int size, unsigned int l, unsigned int x, unsigned int y){
  newCandidates(matrix, size, x, y);
  eliminateCandidatesFromRowAndCol(x, y, size, matrix);
  eliminateCandidatesFromSquare(x, y, l, matrix);
}

void solveSudoku(unsigned int x, unsigned int y, CELL** matrix, unsigned int l, unsigned int size){
  if (y == size) { // end of line
    x++;
    y = 0;
    if(x == size) {  // end
      #pragma omp critical
      {
        print_board(size, matrix);
        exit(EXIT_SUCCESS);
      }
    }
  }
  if (matrix[x][y].value > 0) { // field already set
    solveSudoku(x, y+1, matrix, l, size); // tackle next field
    return;
  }
  resetCandidates(matrix, size, l, x, y);
  for (unsigned int i = 0; i < size; i++) { // try all numbers
    if(matrix[x][y].candidates[i]) {
      if(thread_counter < omp_get_num_threads()) {
        #pragma omp atomic
        thread_counter++;
        CELL **new_matrix = copy_board(size, matrix);
        new_matrix[x][y].value = i + 1;
        #pragma omp task firstprivate(x,y, i, new_matrix)
        {
          solveSudoku(x, y+1, new_matrix, l, size);
          free_board(new_matrix,size);
          #pragma omp atomic
          thread_counter--;
        }
      }
      else{
        matrix[x][y].value = i + 1;
        matrix[x][y].candidates[i]=0;
        solveSudoku(x, y+1, matrix, l, size);
      }
    }
  }
  #pragma omp taskwait
  matrix[x][y].value=0;
  return;
}

int main(int argc, char *argv[]) {
  CELL **matrix = NULL;
  unsigned int l;
  unsigned int size;

  if (argc != MAXARGS) {
    printf("%s\n", "Wrong number of args");
    exit(EXIT_FAILURE);
  }

  matrix = readFromFile(argv[1], &l);
  size = l * l;
  thread_counter = 1;//Counting with master thread
  #pragma omp parallel shared(thread_counter)
  {
    #pragma omp single
    {
      solveSudoku(0, 0, matrix, l, size);
    }
    #pragma omp barrier
  }
  printf("No solution\n");
}
