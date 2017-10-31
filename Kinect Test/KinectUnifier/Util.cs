﻿using System;
using System.Collections.Generic;
using System.Drawing;


namespace KinectUnifier
{
    public static class Util
    {
        public static IDictionary<JointType, Point2F> MapJointsToColorSpace(IBody body, ICoordinateMapper mapper)
        {
            var ret = new Dictionary<JointType, Point2F>();

            foreach (var joint in body.Joints)
            {
                Point3F cameraPoint = joint.Value.Position;
                if (cameraPoint.Z < 0)
                {
                    cameraPoint.Z = 0.1f;
                }

                Point2F colorPoint =
                       mapper.MapCameraPointToColorSpace(joint.Value.Position);

                ret.Add(joint.Key, colorPoint);
            }

            return ret;
        }

        public static Rectangle? TryGetHeadRectangleInColorSpace(IBody body, ICoordinateMapper mapper)
        {
            IJoint headJoint;
            IJoint neckJoint;
            
            if (!body.Joints.TryGetValue(JointType.Head, out headJoint) || !body.Joints.TryGetValue(JointType.Neck, out neckJoint)) return null;
            if (!headJoint.IsTracked || !neckJoint.IsTracked) return null;

            var headJointCameraPos = mapper.MapCameraPointToColorSpace(headJoint.Position);
            var neckJointCameraPos = mapper.MapCameraPointToColorSpace(neckJoint.Position);
            float headNeckDistance = headJointCameraPos.DistanceTo(neckJointCameraPos);
            bool isFaceVertical = Math.Abs(headJointCameraPos.Y - neckJointCameraPos.Y) >
                                  Math.Abs(headJointCameraPos.X - neckJointCameraPos.X);
            var width =  isFaceVertical ? headNeckDistance * 1.75f : headNeckDistance * 2.5f;
            var height = isFaceVertical ? headNeckDistance * 2.5f : headNeckDistance * 1.75f;
            return new Rectangle(
                (int)(headJointCameraPos.X - width / 2), 
                (int)(headJointCameraPos.Y - height / 1.8f), 
                (int)width, 
                (int)height);
        }
    }


    public struct Point4F
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public Point4F(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }

    public struct Point3F
    {
        public float X;
        public float Y;
        public float Z;

        public Point3F(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public struct Point2F
    {
        public float X;
        public float Y;

        public Point2F(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float DistanceTo(Point2F other)
        {
            return (float) Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));
        }
    }
}
