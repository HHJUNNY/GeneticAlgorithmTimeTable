using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GAF;
using GAF.Operators;

namespace GeneticAlgorithmTimeTable
{
    class Program
    {
        public static Random Ran = new Random();
        private static List<List<string>> _studentsDemands;
        private static StreamWriter _writer;

        private static void Main(string[] args)
        {
            // 상수 읽기
            Constants.Instance.Read();

            // 시간표 편성 대상 강좌 읽어오기
            StreamReader reader = new StreamReader("CourseInformation.txt", Encoding.Unicode);
            var courses = ReadCourses(reader);
            reader.Close();
            reader = new StreamReader("StudentDemand.txt", Encoding.Unicode);
            _writer = new StreamWriter(string.Format("{0:yyyy-MM-dd HH/mm/ss}.txt", DateTime.Now), false, Encoding.Unicode);
            _studentsDemands = ReadStudentDemands(reader);
            reader.Close();

            #region 초기 해 생성 부분
            // 초기 해(chromosome) 집합(population)을 수동으로 생성
            var population = new Population(false, true);
            
            // population에 들어갈 chromosome생성
            for (var p = 0; p < Constants.Instance.PopulationSize; p++)
            {
                // chromosome은 하나의 해 즉, TimeTable을 의미한다
                var chromosome = new Chromosome();
                
                // chromosome에 모든 코스에 해당하는 gene이 정해진 순서대로 배치되도록 한다.
                foreach (var course in courses)
                {
                    // Gene은 시간이 할당된 수업을 의미한다
                    // 새로운 CourseGene 객체를 생성하면 시간이 랜덤으로 배정된다.
                    CourseGene courseGene = new CourseGene(course);
                    
                    // Gene List에 추가
                    chromosome.Genes.Add(new Gene(courseGene));
                }

                // 가능해를 population에 더해준다.
                population.Solutions.Add(chromosome);
            }
            #endregion

            #region 세대 진행 연산 부분

            // 엘리트 연산 - ElitismPercentage
            var elite = new Elite(Constants.Instance.ElitismPercentage);

            // 이점 교차 연산 - CrossoverProbability
            var crossover = new Crossover(Constants.Instance.CrossoverProbability, true, CrossoverType.DoublePoint, ReplacementMethod.GenerationalReplacement);

            // 돌연변이 연산
            var mutate = new CustomMutate(Constants.Instance.MutateProbability);

            // 대체 연산
            var replace = new CustomRandomReplace(Constants.Instance.RandomReplaceProbability, courses);

            // GeneticAlgorithm 생성
            var ga = new GeneticAlgorithm(population, CalculateFitness);

            //hook up to some useful events
            ga.OnGenerationComplete += ga_OnGenerationComplete;
            ga.OnRunComplete += ga_OnRunComplete;

            //add the operators
            ga.Operators.Add(elite);
            ga.Operators.Add(crossover);
            ga.Operators.Add(mutate);
            ga.Operators.Add(replace);

            string line = string.Format("Algorithm start: {0}", DateTime.Now);
            WriteLine(line);
            line = "Generation\tFittest\tAverage\tMedian\t";
            WriteLine(line);

            //run the GA
            ga.Run(Terminate);
            _writer.Close();

            Console.ReadLine();

            #endregion
        }

        /// <summary>
        /// 교수자 - 강좌인덱스(chromosome에서의 순서) map
        /// </summary>
        static Dictionary<string, List<int>> _mapCourseByTeacher = new Dictionary<string, List<int>>();

        /// <summary>
        /// 학년 - 강좌인덱스(chromosome에서의 순서) map
        /// </summary>
        static Dictionary<int, List<int>> _mapCourseByYear = new Dictionary<int, List<int>>();

        /// <summary>
        /// 강좌 ID - 강좌 인덱스 map
        /// </summary>
        static Dictionary<string, List<int>> _mapCourseByID = new Dictionary<string, List<int>>();

        /// <summary>
        /// 강좌 정보를 읽어와서 리스트로 만듭니다.
        /// </summary>
        private static List<Course> ReadCourses(StreamReader reader)
        {
            var courses = new List<Course>();

            reader.ReadLine();  // 첫줄은 column name이므로 건너뛰기
            while (reader.Peek() >= 0)
            {
                string line = reader.ReadLine();
                courses.Add(new Course(line, '\t'));
            }

            // fixedTime이 있는 강좌를 앞으로 보낸다
            courses.Sort((x, y) => y.FixedTime.CompareTo(x.FixedTime));
           
            // 제약 조건 검사를 위한 매핑 테이블 생성
            for (int i = 0; i < courses.Count; ++i)
            {
                // 교수자-과목 매핑
                List<int> courseIndices1;
                if (_mapCourseByTeacher.TryGetValue(courses[i].Teacher, out courseIndices1))
                {
                    courseIndices1.Add(i);
                }
                else
                {
                    courseIndices1 = new List<int> { i };
                    _mapCourseByTeacher.Add(courses[i].Teacher, courseIndices1);
                }

                // 학년-과목 매핑
                List<int> courseIndices2;
                if (_mapCourseByYear.TryGetValue(courses[i].Year, out courseIndices2))
                {
                    courseIndices2.Add(i);
                }
                else
                {
                    courseIndices2 = new List<int> { i };
                    _mapCourseByYear.Add(courses[i].Year, courseIndices2);
                }

                // ID-과목 매핑
                List<int> courseIndices3;
                if (_mapCourseByID.TryGetValue(courses[i].ID, out courseIndices3))
                {
                    courseIndices3.Add(i);
                }
                else
                {
                    courseIndices3 = new List<int> { i };
                    _mapCourseByID.Add(courses[i].ID, courseIndices3);
                }
            }

            // ID-과목 매핑에서, 하나의 ID에 종속된 모든 분반이 같은 시간에 forced 되어있다면, 하나만 남기고 지워준다
            foreach(List<int> courseList in _mapCourseByID.Values)
            {
                string fixedTime = courses[courseList[0]].FixedTime;
                if (fixedTime == string.Empty)
                    continue;

                bool isAllSameFixedTime = courseList.ConvertAll<Course>(x => courses[x]).All(x => x.FixedTime == fixedTime);
                if (isAllSameFixedTime)
                {
                    int firstCourse = courseList[0];
                    courseList.Clear();
                    courseList.Add(firstCourse);
                }
            }

            return courses;
        }

        /// <summary>
        /// 학생 수요 정보를 읽어옵니다.
        /// </summary>
        private static List<List<string>> ReadStudentDemands(StreamReader reader)
        {
            var students = new List<List<string>>();
            
            while (reader.Peek() >= 0)
            {
                string line = reader.ReadLine();
                string[] tokens = line.Split('\t');
                List<string> demands = new List<string>();
                for (int i = 2; i < tokens.Length; i+= 2)
                {
                    if (tokens[i].Length > 0)
                        demands.Add(tokens[i].Trim());
                }
                students.Add(demands);
            }

            return students;
        }

        /// <summary>
        /// 특정 해가 강한 제약조건을 만족하는지 여부를 반환한다.
        /// </summary>
        private static bool CheckValidity(Chromosome chromosome)
        {
            // 강한 제약 조건 1. 같은 교수자일 경우 동일 시간을 피한다.
            foreach (List<int> sameTeacherCourses in _mapCourseByTeacher.Values)
            {
                if (sameTeacherCourses.Count > 1)
                {
                    for (int i = 0; i < sameTeacherCourses.Count; ++i)
                    {
                        CourseGene lhs = (CourseGene)chromosome.Genes[sameTeacherCourses[i]].ObjectValue;
                        for (int j = i + 1; j < sameTeacherCourses.Count; ++j)
                        {
                            CourseGene rhs = (CourseGene)chromosome.Genes[sameTeacherCourses[j]].ObjectValue;
                            if (true == lhs.IsOverlap(rhs))
                                return false;
                        }
                    }
                }
            }

            // 강한 제약 조건 2. 전필 과목에 한해서 같은 학년 수업이고 동일 과목이 아닌 경우 동일 시간을 피한다.
            foreach (List<int> sameYearCourses in _mapCourseByYear.Values)
            {
                if (sameYearCourses.Count > 1)
                {
                    for (int i = 0; i < sameYearCourses.Count; ++i)
                    {
                        CourseGene lhs = (CourseGene)chromosome.Genes[sameYearCourses[i]].ObjectValue;
                        if (false == lhs.CourseInfo.IsMandatory)
                            continue;
                        for (int j = i + 1; j < sameYearCourses.Count; ++j)
                        {
                            CourseGene rhs = (CourseGene)chromosome.Genes[sameYearCourses[j]].ObjectValue;
                            if (lhs.CourseInfo.ID != rhs.CourseInfo.ID && true == lhs.IsOverlap(rhs))
                                return false;
                        }
                    }
                }
            }

            // 강한 제약 조건 3. 전필 과목은 골고루 요일이 골고루 분포하도록 배치
            foreach (List<int> sameIDCourses in _mapCourseByID.Values)
            {
                if (sameIDCourses.Count > 1)
                {
                    if (false == ((CourseGene)chromosome.Genes[sameIDCourses[0]].ObjectValue).CourseInfo.IsMandatory)
                        continue;

                    List<CourseDay> neccessaryDays = new List<CourseDay> { CourseDay.AC, CourseDay.BD };
                    for (int i = 0; i < sameIDCourses.Count; ++i)
                    {
                        neccessaryDays.Remove((CourseDay)((CourseGene)chromosome.Genes[sameIDCourses[i]].ObjectValue).Day);
                    }

                    if (neccessaryDays.Count > 0)
                        return false;
                }
            }

            return true;
        }

        static void WriteLine(string line)
        {
            Console.WriteLine(line);
            _writer.WriteLine(line);
        }

        static void ga_OnRunComplete(object sender, GaEventArgs e)
        {
            string line = string.Format("Algorithm end: {0}", DateTime.Now);
            WriteLine(line);

            var fittest = e.Population.GetTop(1)[0];

            WriteLine("");

            WriteLine("Fittest solution - fitness \t" + fittest.Fitness);
            foreach (var gene in fittest.Genes)
            {
                WriteLine(((CourseGene)gene.ObjectValue).ToString());
            }
        }

        private static void ga_OnGenerationComplete(object sender, GaEventArgs e)
        {
            var fittest = e.Population.GetTop(1)[0];
            var median = e.Population.GetTopPercent(50)[0];
            string line = string.Format("{0}\t{1:0.0000}\t{2:0.0000}\t{3:0.0000}", e.Generation, fittest.Fitness, e.Population.AverageFitness, median.Fitness);
            WriteLine(line);
        }

        /// <summary>
        /// 적합도를 계산합니다.
        /// </summary>
        private static double CalculateFitness(Chromosome chromosome)
        {
            double fitness = 1;
            if (false == CheckValidity(chromosome))
                fitness -= 0.5;

            foreach (List<string> studentDemand in _studentsDemands)
            {
                double penalty = 0;                 // 학생 개인 벌점(0~1)
                List<List<CourseGene>> demandedCourses = new List<List<CourseGene>>();
                foreach (string courseID in studentDemand)
                {
                    if (_mapCourseByID.ContainsKey(courseID))
                    {
                        List<int> courseIndex = _mapCourseByID[courseID];
                        demandedCourses.Add(courseIndex.ConvertAll<CourseGene>(x => (CourseGene)chromosome.Genes[x].ObjectValue));
                    }
                }


                if (demandedCourses.Count > 1)
                {
                    // 1차 분모 = 각 course의 분반 수의 곱
                    double n1 = 1;
                    demandedCourses.ForEach(x => n1 *= x.Count);
                    // 2차 분모 = 각 course들이 중첩되는 순서쌍의 최대 조합
                    double n2 = demandedCourses.Count * (demandedCourses.Count - 1) / 2;

                    double overlapCount = 0;
                    for (int i = 0; i < demandedCourses.Count; ++i)
                    {
                        for (int j = i + 1; j < demandedCourses.Count; ++j)
                        {
                            foreach (CourseGene lhs in demandedCourses[i])
                            {
                                foreach (CourseGene rhs in demandedCourses[j])
                                {
                                    if (lhs.IsOverlap(rhs))
                                    {
                                        overlapCount += n1 / demandedCourses[i].Count / demandedCourses[j].Count;
                                    }
                                }
                            }
                        }
                    }                    

                    penalty = overlapCount / (n1 * n2);                    
                }

                penalty += CalculateOverHourPenalty(new List<CourseGene>(), 0, demandedCourses);

                fitness -= penalty / _studentsDemands.Count;
            }

            return fitness;
        }

        /// <summary>
        /// 하루 4시간 이상 수업에 대한 벌점을 계산하는 재귀함수. 벌점 값 0~1
        /// </summary>
        private static double CalculateOverHourPenalty(List<CourseGene> timeTable, int index, List<List<CourseGene>> demandedCourses)
        {
            if (index == demandedCourses.Count)
            {
                int over4HourDayCount = 0;
                double[] hoursOfDay = new double[5];        // 각 날에 수업이 몇시간 있는지 세기 위한 배열
                for (int i = 0; i < timeTable.Count; ++i)
                {
                    switch (timeTable[i].Day)
                    {
                        case CourseDay.A:
                        case CourseDay.B:
                        case CourseDay.C:
                        case CourseDay.D:
                        case CourseDay.E:
                            {
                                hoursOfDay[(int)timeTable[i].Day] += timeTable[i].ClassHoursOfWeek[0].Item2 - timeTable[i].ClassHoursOfWeek[0].Item1;
                            }
                            break;
                        case CourseDay.AC:
                        case CourseDay.BD:
                            {
                                hoursOfDay[(int)(timeTable[i].Day - CourseDay.AC)] += timeTable[i].ClassHoursOfWeek[0].Item2 - timeTable[i].ClassHoursOfWeek[0].Item1;
                                hoursOfDay[(int)(timeTable[i].Day - CourseDay.AC + CourseDay.C)] += timeTable[i].ClassHoursOfWeek[1].Item2 - timeTable[i].ClassHoursOfWeek[1].Item1;
                            }
                            break;
                    }
                }

                for (int i =0;i < hoursOfDay.Length; ++i)
                {
                    if (hoursOfDay[i] >= 4d)
                        over4HourDayCount++;
                }

                return (double)over4HourDayCount / 30;
            }
            else
            {
                double penalty = 0;
                for (int i = 0; i < demandedCourses[index].Count; ++i)
                {
                    timeTable.Add(demandedCourses[index][i]);
                    penalty += CalculateOverHourPenalty(timeTable, index + 1, demandedCourses);
                    timeTable.RemoveAt(timeTable.Count - 1);
                }

                return penalty / demandedCourses[index].Count;
            }
        }

        /// <summary>
        /// 탈출 조건을 계산합니다.
        /// </summary>
        public static bool Terminate(Population population, int currentGeneration, long currentEvaluation)
        {            
            return currentGeneration >= Constants.Instance.TerminateGeneration || 
            population.GetTop(1)[0].Fitness > Constants.Instance.TerminateFitness;
        }
    }
}
