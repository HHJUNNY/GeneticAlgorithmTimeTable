using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmTimeTable
{    
    class Course
    {
        public int ID { private set; get; }                 // 강좌 번호
        public string Name { private set; get; }            // 강좌 이름
        public int Credit { private set; get; }             // 학점 수
        public CourseDay Day { private set; get; }          // 요일
        public CoursePeriod Period { private set; get; }    // 시작 시간
    }

    /// <summary>
    /// 수업 요일
    /// </summary>
    enum CourseDay
    {
        MondayWednesday,        // 월수
        TuesdayThursday,        // 화목
        Friday,                 // 금
    }

    /// <summary>
    /// 수업 시작 시간
    /// </summary>
    enum CoursePeriod
    {
        _0,     // 0 교시, 08시~09시
        _1,     // 1 교시, 09시~10시
        _2,     // 2 교시, 10시~11시
        _3,     // 3 교시, 11시~12시
        _4,     // 4 교시, 12시~13시
        _5,     // 5 교시, 13시~14시
        _6,     // 6 교시, 14시~15시
        _7,     // 7 교시, 15시~16시
        _8,     // 8 교시, 16시~17시
        _9,     // 9 교시, 17시~18시
        _10,    // 10 교시, 18시~19시
        _11,    // 11 교시, 19시~20시
        // 추가바람
    }
}
