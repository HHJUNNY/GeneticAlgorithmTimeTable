using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmTimeTable
{
    class CourseGene
    {
        public Course CourseInfo { private set; get; }      // 강좌 정보

        public CourseDay? Day { private set; get; }          // 요일
        public double? Period { private set; get; }          // 시작 시간
        
        // 수업 시간을 <시작, 끝> 순서쌍의 모음으로 표현한 것.
        // 월요일 0시를 0으로, 1=1시간으로 표현. 화요일 0시는 24, 수요일 0시는 48, ...
        // 즉, 월수 9:30 ~ 11:00의 경우 <9.5, 11>, <57.5, 59> 로 표현됨
        public List<Tuple<double, double>> ClassHoursOfWeek { private set; get; }

        public CourseGene(Course courseInfo)
        {
            // 강좌의 시간 외적인 정보
            CourseInfo = courseInfo;

            // Gene이 처음으로 생성될 때 시간 랜덤 배정
            GenerateRandomTime();
        }

        /// <summary>
        /// 랜덤으로 시간 배정
        /// </summary>
        public void GenerateRandomTime()
        {
            // fixed time이 있을 경우 그대로 배정하고, 없을 경우 랜덤 배정
            if (CourseInfo.FixedTime == String.Empty)
            {
                // 학점 수에 따라 다른 방식
                switch (CourseInfo.TheoryCredit)
                {
                    case 3:
                        // 이론 3학점 수업: 월수/화목 1.5시간씩, 정해진 시간대 중 랜덤으로
                        {
                            // 요일 결정
                            int randomIndex = Program.Ran.Next(Constants.Instance.AvailableDay_3Credit.Count);
                            Day = Constants.Instance.AvailableDay_3Credit[randomIndex];
                            // 시간 결정
                            randomIndex = Program.Ran.Next(Constants.Instance.AvailablePeriod_3Credit.Count);
                            Period = Constants.Instance.AvailablePeriod_3Credit[randomIndex];
                        }
                        break;
                    default:
                        // 해당하지 않을 경우 에러
                        {
                            throw new Exception("시간 배정 에러");
                        }
                }
            }
            else
            {
                // Fixed Time이 있는 경우는 Day나 Period가 배정 안됐을때만 배정해주면 됨
                if (Day == null && Period == null)
                {
                    // FixedTime은 "BD 1530-1700", "C 0930-1130" 식으로 들어옴. 파싱해준다.
                    string[] tokens = CourseInfo.FixedTime.Split(' ');
                    Day = (CourseDay)Enum.Parse(typeof(CourseDay), tokens[0]);
                    Period = Double.Parse(tokens[1].Substring(0, 2)) + Double.Parse(tokens[1].Substring(2, 2)) / 60d;
                }
            }

            SetClassTime();
        }

        /// <summary>
        /// Day와 Period를 m_classTimesOfWeek에 변환하여 저장
        /// </summary>
        private void SetClassTime()
        {
            ClassHoursOfWeek = new List<Tuple<double, double>>();
            switch (Day)
            {
                case CourseDay.A:
                case CourseDay.B:
                case CourseDay.C:
                case CourseDay.D:
                case CourseDay.E:
                    {
                        // 하루짜리 수업은 하나의 Tuple로 구성. 수업 길이 = 이론학점수.
                        ClassHoursOfWeek.Add(new Tuple<double, double>(
                            (double)(Day) * 24d + (double)Period,
                            (double)(Day) * 24d + (double)Period + CourseInfo.TheoryCredit
                            ));
                    }
                    break;
                case CourseDay.AC:
                case CourseDay.BD:
                    {
                        // 이틀짜리 수업은 두개의 Tuple로 구성. 수업 길이 = 이론학점수/2.
                        ClassHoursOfWeek.Add(new Tuple<double, double>(
                            (double)(Day - CourseDay.AC) * 24d + (double)Period,
                            (double)(Day - CourseDay.AC) * 24d + (double)Period + CourseInfo.TheoryCredit / 2d
                            ));
                        ClassHoursOfWeek.Add(new Tuple<double, double>(
                            (double)(Day - CourseDay.AC + CourseDay.C) * 24d + (double)Period,
                            (double)(Day - CourseDay.AC + CourseDay.C) * 24d + (double)Period + CourseInfo.TheoryCredit / 2d
                            ));
                    }
                    break;
            }
        }

        /// <summary>
        /// 이 객체와 target 객체의 수업 시간이 겹치는지 여부를 반환
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool IsOverlap(CourseGene target)
        {
            int i = 0, j = 0;
            do
            {
                Tuple<double, double> x = ClassHoursOfWeek[i];
                Tuple<double, double> y = target.ClassHoursOfWeek[j];
                if (x.Item1 <= y.Item1)
                {
                    if (x.Item2 >= y.Item1) { return true; }
                    ++i;
                }
                else
                {
                    if (x.Item1 <= y.Item2) { return true; }
                    ++j;
                }
            } while (i < ClassHoursOfWeek.Count && j < target.ClassHoursOfWeek.Count);

            return false;
        }
    }

    /// <summary>
    /// 수업 요일
    /// </summary>
    public enum CourseDay
    {
        A,                 // 월
        B,                 // 화
        C,                 // 수
        D,                 // 목
        E,                 // 금
        AC,                // 월수
        BD,                // 화목
    }
}
