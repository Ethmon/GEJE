using System;

namespace GEJE
{
    class Rotation
    {
        public static double WrapAngle(double angle)
        {
            return (angle + 360) % 360 < 0 ? (angle + 360) % 360 : angle % 360;
        }
        static double DegToRad(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        // Get rotation matrix for rotation around the x-axis
        public static double[,] GetRotationMatrixX(double angle)
        {
            double cosTheta = Math.Cos(angle);
            double sinTheta = Math.Sin(angle);

            double[,] rotationMatrix = {
                {1, 0, 0, 0},
                {0, cosTheta, -sinTheta, 0},
                {0, sinTheta, cosTheta, 0},
                {0, 0, 0, 1}
            };

            return rotationMatrix;
        }

        // Get rotation matrix for rotation around the y-axis
        public static double[,] GetRotationMatrixY(double angle)
        {
            double cosTheta = Math.Cos(angle);
            double sinTheta = Math.Sin(angle);

            double[,] rotationMatrix = {
                {cosTheta, 0, sinTheta, 0},
                {0, 1, 0, 0},
                {-sinTheta, 0, cosTheta, 0},
                {0, 0, 0, 1}
            };

            return rotationMatrix;
        }

        // Get rotation matrix for rotation around the z-axis
        public static double[,] GetRotationMatrixZ(double angle)
        {
            double cosTheta = Math.Cos(angle);
            double sinTheta = Math.Sin(angle);

            double[,] rotationMatrix = {
                {cosTheta, -sinTheta, 0, 0},
                {sinTheta, cosTheta, 0, 0},
                {0, 0, 1, 0},
                {0, 0, 0, 1}
            };

            return rotationMatrix;
        }

        // Combine multiple transformation matrices
        public static double[,] CombineMatrices(double[,] matrix1, double[,] matrix2)
        {
            int rows1 = matrix1.GetLength(0);
            int cols1 = matrix1.GetLength(1);
            int rows2 = matrix2.GetLength(0);
            int cols2 = matrix2.GetLength(1);

            if (cols1 != rows2)
            {
                throw new ArgumentException("Number of columns of the first matrix must be equal to the number of rows of the second matrix.");
            }

            double[,] result = new double[rows1, cols2];

            for (int i = 0; i < rows1; i++)
            {
                for (int j = 0; j < cols2; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < cols1; k++)
                    {
                        sum += matrix1[i, k] * matrix2[k, j];
                    }
                    result[i, j] = sum;
                }
            }

            return result;
        }

        // Apply rotation to a point in homogeneous coordinates
        public static double[] ApplyRotation(double[,] rotationMatrix, double x, double y, double z)
        {
            double[] result = new double[4];

            result[0] = rotationMatrix[0, 0] * x + rotationMatrix[0, 1] * y + rotationMatrix[0, 2] * z + rotationMatrix[0, 3];
            result[1] = rotationMatrix[1, 0] * x + rotationMatrix[1, 1] * y + rotationMatrix[1, 2] * z + rotationMatrix[1, 3];
            result[2] = rotationMatrix[2, 0] * x + rotationMatrix[2, 1] * y + rotationMatrix[2, 2] * z + rotationMatrix[2, 3];
            result[3] = rotationMatrix[3, 0] * x + rotationMatrix[3, 1] * y + rotationMatrix[3, 2] * z + rotationMatrix[3, 3];

            return result;
        }
    }
}
