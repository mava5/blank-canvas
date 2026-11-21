
using UnityEngine;


namespace ObjectDrawing
{
    public struct DrawOperation
    {
        public readonly Matrix4x4 ProjectionMatrix => projectionMatrix;
        public readonly bool IsOrthographic => isOrthographic;
        public readonly float ZNear => zNear;
        public readonly float ZFar => zFar;

        public Brush Brush;

        /// <summary> World position of camera. </summary>
        public Vector3 Position
        {
            readonly get => position;
            set { position = value; UpdateViewMatrix(); }
        }

        /// <summary> World rotation of camera. </summary>
        public Quaternion Rotation
        {
            readonly get => rotation;
            set { rotation = value; UpdateViewMatrix(); }
        }

        /// <summary> World->camera view matrix of camera. </summary>
        public Matrix4x4 ViewMatrix
        {
            readonly get => viewMatrix;
            set { viewMatrix = value; UpdatePositionRotation(); }
        }


        private Vector3 position;
        private Quaternion rotation;
        private Matrix4x4 viewMatrix;

        private readonly Matrix4x4 projectionMatrix;
        private readonly bool isOrthographic;
        private readonly float zNear;
        private readonly float zFar;


        public DrawOperation(Brush brush, Vector3 position, Quaternion rotation, Matrix4x4 projectionMatrix, bool isOrthographic, float zNear, float zFar)
        {
            Brush = brush;
            this.position = position;
            this.rotation = rotation;
            viewMatrix = GetWorldToCameraMatrix(position, rotation);
            this.projectionMatrix = projectionMatrix;
            this.isOrthographic = isOrthographic;
            this.zNear = zNear;
            this.zFar = zFar;
        }

        public DrawOperation(Brush brush, Vector3 position, Vector3 eulerAngles, Matrix4x4 projectionMatrix, bool isOrthographic, float zNear, float zFar)
        {
            Brush = brush;
            this.position = position;
            rotation = Quaternion.Euler(eulerAngles);
            viewMatrix = GetWorldToCameraMatrix(position, rotation);
            this.projectionMatrix = projectionMatrix;
            this.isOrthographic = isOrthographic;
            this.zNear = zNear;
            this.zFar = zFar;
        }

        public DrawOperation(Brush brush, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, bool isOrthographic, float zNear, float zFar)
        {
            Brush = brush;
            position = viewMatrix.GetPosition();
            rotation = viewMatrix.rotation;
            this.viewMatrix = viewMatrix;
            this.projectionMatrix = projectionMatrix;
            this.isOrthographic = isOrthographic;
            this.zNear = zNear;
            this.zFar = zFar;
        }


        public static DrawOperation Orthographic(Brush brush, Vector3 position, Quaternion rotation, float size, float zNear = 0.1f, float zFar = 1000f)
        {
            float halfSize = size / 2f;
            return new(brush, position, rotation, Matrix4x4.Ortho(-halfSize, halfSize, -halfSize, halfSize, zNear, zFar), true, zNear, zFar);
        }

        public static DrawOperation Orthographic(Brush brush, Vector3 position, Vector3 eulerAngles, float size, float zNear = 0.1f, float zFar = 1000f)
        {
            float halfSize = size / 2f;
            return new(brush, position, eulerAngles, Matrix4x4.Ortho(-halfSize, halfSize, -halfSize, halfSize, zNear, zFar), true, zNear, zFar);
        }

        public static DrawOperation Orthographic(Brush brush, Matrix4x4 viewMatrix, float size, float zNear = 0.1f, float zFar = 1000f)
        {
            float halfSize = size / 2f;
            return new(brush, viewMatrix, Matrix4x4.Ortho(-halfSize, halfSize, -halfSize, halfSize, zNear, zFar), true, zNear, zFar);
        }

        public static DrawOperation Perspective(Brush brush, Vector3 position, Quaternion rotation, float fov, float zNear = 0.1f, float zFar = 1000f)
        { return new(brush, position, rotation, Matrix4x4.Perspective(fov, 1f, zNear, zFar), false, zNear, zFar); }

        public static DrawOperation Perspective(Brush brush, Vector3 position, Vector3 eulerAngles, float fov, float zNear = 0.1f, float zFar = 1000f)
        { return new(brush, position, eulerAngles, Matrix4x4.Perspective(fov, 1f, zNear, zFar), false, zNear, zFar); }

        public static DrawOperation Perspective(Brush brush, Matrix4x4 viewMatrix, float fov, float zNear = 0.1f, float zFar = 1000f)
        { return new(brush, viewMatrix, Matrix4x4.Perspective(fov, 1f, zNear, zFar), false, zNear, zFar); }


        private void UpdateViewMatrix()
        { viewMatrix = GetWorldToCameraMatrix(position, rotation); }

        private void UpdatePositionRotation()
        {
            // Convert world->camera view matrix back to camera->world
            // This way the position and rotation appear correctly in world space
            Matrix4x4 trs = viewMatrix;
            trs.m20 = -trs.m20;
            trs.m21 = -trs.m21;
            trs.m22 = -trs.m22;
            trs.m23 = -trs.m23;

            trs = trs.inverse;

            position = trs.GetColumn(3);
            rotation = trs.rotation;
        }

        private static Matrix4x4 GetWorldToCameraMatrix(Vector3 pos, Quaternion rot)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(pos, rot, Vector3.one).inverse;

            matrix.m20 = -matrix.m20;
            matrix.m21 = -matrix.m21;
            matrix.m22 = -matrix.m22;
            matrix.m23 = -matrix.m23;

            return matrix;
        }
    }
}
