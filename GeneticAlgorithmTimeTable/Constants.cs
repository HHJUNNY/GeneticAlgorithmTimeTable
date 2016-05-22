using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmTimeTable
{
    /// <summary>
    /// 알고리즘에 사용되는 상수들을 모아놓은 클래스
    /// </summary>
    class Constants
    {
        #region Singleton Pattern
        private static Constants instance;

        private Constants() { Read(); }

        public static Constants Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Constants();
                }
                return instance;
            }
        }
        #endregion

        /// <summary>
        /// 이론 2~3학점 수업이 시작 가능한 시간
        /// </summary>
        public List<double> AvailablePeriod_23Credit = new List<double> { 9.5, 11.0, 14.0, 15.5, 17.0 };

        /// <summary>
        /// 이론 3학점 수업이 선택 가능한 요일
        /// </summary>
        public List<CourseDay> AvailableDay_3Credit = new List<CourseDay> { CourseDay.AC, CourseDay.BD };

        /// <summary>
        /// 이론 2학점 수업이 선택 가능한 요일
        /// </summary>
        public List<CourseDay> AvailableDay_2Credit = new List<CourseDay> { CourseDay.A, CourseDay.B, CourseDay.C, CourseDay.D };

        /// <summary>
        /// 한 세대 인구 수
        /// </summary>
        public int PopulationSize { get; private set; }

        /// <summary>
        /// 엘리트 연산 선택 % 수
        /// </summary>
        public int ElitismPercentage { get; private set; }

        /// <summary>
        /// 교차 연산 %
        /// </summary>
        public double CrossoverProbability { get; private set; }

        /// <summary>
        /// 돌연변이 %
        /// </summary>
        public double MutateProbability { get; private set; }
        
        private void Read()
        {
            PopulationSize = 200;
            ElitismPercentage = 5;
            CrossoverProbability = 0.8;
            MutateProbability = 0.002;
        }
    }
}
