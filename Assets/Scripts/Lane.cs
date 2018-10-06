using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rhythm {

    public struct BezierPoint {
        public Vector3 controlBefore;
        public Vector3 point;
        public Vector3 controlAfter;
        public bool isBezier;

        public BezierPoint(Vector3 point){
            this.point = point;
            this.controlBefore = point;
            this.controlAfter = point;
            this.isBezier = false;
        }

        public static implicit operator Vector3(BezierPoint point){
            return point.point;
        }
        public static implicit operator BezierPoint(Vector3 vector3){
            return new BezierPoint(vector3);
        }
    }

    public class Lane {
        public static int LaneMax = 5;
        public float durationMs = 800f; //MSec

        private List<BezierPoint[]> pointList = new List<BezierPoint[]>();
        private Vector3[] baseStartPoints = { 
            new Vector3(-4, 8, 0), 
            new Vector3(-2, 8, 0), 
            new Vector3(0, 8, 0), 
            new Vector3(2, 8, 0), 
            new Vector3(4, 8, 0) 
        };
        private Vector3[] baseEndPoints = { 
            new Vector3(-4, -3, 0), 
            new Vector3(-2, -3, 0), 
            new Vector3(0, -3, 0), 
            new Vector3(2, -3, 0), 
            new Vector3(4, -3, 0) 
        };
        private BezierPoint[] startPoints = new BezierPoint[5];
        private BezierPoint[] endPoints = new BezierPoint[5];


        public BezierPoint[] StartPoints {
            get {
                return pointList[0];
            }
        }
        public BezierPoint[] EndPoints {
            get {
                return pointList[pointList.Count - 1];
            }
        }

        public Lane (){
            for (int i = 0; i < baseStartPoints.Length;i++){
                startPoints[i] = new BezierPoint(baseStartPoints[i]);
                endPoints[i] = new BezierPoint(baseEndPoints[i]);
            }
            pointList.Add(startPoints);
            pointList.Add(endPoints);
        }

        public Vector3 GetLanePos(int laneIndex, float laneLerpPos){
            // TODO : ベジェ曲線対応

            //0~1以外だったら始点or終点を返す
            // TODO : 終点以降(1~)も適切な座標を返したい、例えば直前ポイントと終点のベクトルを取って反転するとか。処理重いかも？
            if (laneLerpPos <= 0){
                return pointList[0][laneIndex].point;
            } else if (laneLerpPos >= 1){
                return pointList[pointList.Count - 1][laneIndex].point;
            }

            //Debug.Log("s : " + pointList[Mathf.FloorToInt(laneLerpPos)][laneIndex].point + ", e : " + pointList[Mathf.FloorToInt(laneLerpPos) + 1][laneIndex].point);
            laneLerpPos *= (pointList.Count - 1); //ポイント数分
            
            return LerpVector3(pointList[Mathf.FloorToInt(laneLerpPos)][laneIndex].point, pointList[Mathf.FloorToInt(laneLerpPos) + 1][laneIndex].point, laneLerpPos - Mathf.Floor(laneLerpPos));
        }

        public Vector3 LerpVector3(Vector3 s, Vector3 e, float t){
            return new Vector3(s.x + (e.x - s.x) * t, s.y +  (e.y - s.y) * t, s.z + (e.z - s.z) * t);
        }
    }
}