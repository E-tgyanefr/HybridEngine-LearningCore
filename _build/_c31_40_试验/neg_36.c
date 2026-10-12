#include <stdio.h>
#include <math.h>
int main(void) {
    printf("sqrt(16) = %f\n", sqrt(16.0));
    printf("sqrt(2) = %f\n", sqrt(2.0));
    printf("pow(2, 10) = %f\n", pow(2.0, 10.0));
    printf("pow(3, 3) = %f\n", pow(3.0, 3.0));
    printf("fabs(-2.5) = %f\n", fabs(-2.5));
    printf("floor(2.5) = %f\n", (double)(int)(2.5));
    printf("floor(-2.5) = %f\n", (double)(int)(-2.5));
    printf("ceil(2.1) = %f\n", ceil(2.1));
    return 0;
}