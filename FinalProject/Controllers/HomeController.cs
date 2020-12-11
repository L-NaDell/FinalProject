﻿using FinalProject.Models;
using FinalProject.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FinalProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HighlightService _highlightService;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            _highlightService = new HighlightService();
        }

        public IActionResult Index()
        {
            return View();
        }

        //figure out what list to display
        public IActionResult RecentHighlights(int? page)
        {
            List<List<Highlight>> list = _highlightService.GetHighlights();
            if (page == null)
            {
                page = 1;
            }
            ViewBag.pageCount = page;
            ViewBag.listCount = list.Count;
            return View(list[(int)page - 1]);
        }

        public IActionResult SearchHighlights(string searchFor, int? page)
        {
            List<Highlight> highlights = FootballDAL.GetHighlights();
            List<Highlight> searchResults = new List<Highlight> { };

            foreach (var video in highlights)
            {
                if (video.competition.name.ToLower().Contains(searchFor.ToLower()))
                {
                    searchResults.Add(video);
                }
                if (video.title.ToLower().Contains(searchFor.ToLower()))
                {
                    searchResults.Add(video);
                }
            }
            List<List<Highlight>> list = _highlightService.SplitList(searchResults);
            if (page == null)
            {
                page = 1;
            }
            ViewBag.pageCount = page;
            ViewBag.listCount = list.Count;
            var beautifiedSearch = string.Join(" ", searchFor.ToLower().Split(" ").Select(word => $"{char.ToUpper(word[0])}{word.Substring(1)}"));
            ViewBag.Search = beautifiedSearch;
            return View(list[(int)page - 1]);
        }
        [HttpPost]
        public IActionResult LeagueStandings(string league, string season)
        {
            FootballStandings standings = FootballDAL.GetStandings(league, season);
            return View(standings);
        }

        [HttpPost]
        public IActionResult LeagueTeams(string league, string season)
        {
            FootballClubs clubs = FootballDAL.GetTeams(league, season);
            return View(clubs);
        }

        public IActionResult MatchResults(string league, string season)
        {

            FootballMatches clubs = FootballDAL.GetMatches(league, season);
            return View(clubs);
        }

        // Grabbing scores based on rank difference (in standings) and current form (last five games results)
        public IActionResult Probability(string league, string team1, string team2)
        {
            // Translates between different APIs (standings api and league/results api)
            switch (league)
            {
                case "Premier League 2020/21":
                    league = "39";
                    team1 = ConvertTeamEnglish(team1);
                    team2 = ConvertTeamEnglish(team2);
                    break;
                //case "Primera División 2020/21";
                //    team1 = ConvertTeamEnglish(team1);
                //    team2 = ConvertTeamEnglish(team2);
                default:
                    break;
            }

            // Calls the standings API based on league of teams playing
            var results = FootballDAL.GetStandings(league, "2020");
            var standings = results.response[0].league.standings[0];
            List<Standing> standingslist = new List<Standing>();

            // Filtering through standings and grabbing selected teams (from match results view)
            foreach (var item in standings)
            {
                if (item.team.name == team1 || item.team.name == team2)
                {
                    standingslist.Add(item);
                }
            }

            //Checks current form scores and adds according to results (W, D, L)
            string team1Form = standingslist[0].form;
            string team2Form = standingslist[1].form;
            double team1FormScore = 0;
            double team2FormScore = 0;
            foreach (char f in team1Form)
            {
                if (f == 'W')
                {
                    team1FormScore += 2;
                }
                else if (f == 'D')
                {
                    team1FormScore += 1;
                }
            }
            foreach (char f in team2Form)
            {
                if (f == 'W')
                {
                    team2FormScore += 2;
                }
                else if (f == 'D')
                {
                    team2FormScore += 1;
                }
            }

            // Checks current rank difference between two teams
            int team1Rank = standingslist[0].rank;
            int team2Rank = standingslist[1].rank;
            double team1RankScore = 0;
            double team2RankScore = 0;
            double difference = 0;

            // Statements to prevent negative number result
            if (team1Rank < team2Rank)
            {
                difference = team2Rank - team1Rank;
                team1RankScore = difference / standings.Length * 10;
                team2RankScore = difference / standings.Length * 5;
            }
            else
            {
                difference = team1Rank - team2Rank;
                team1RankScore = difference / standings.Length * 5;
                team2RankScore = difference / standings.Length * 10;
            }

            int team1GD = standingslist[0].goalsDiff;
            int team2GD = standingslist[1].goalsDiff;

            double team1GDScore = 0;
            double team2GDScore = 0;

            if (team1GD != team2GD)
            {
                if (team1GD > team2GD)
                {
                    int teamsGD = team1GD - team2GD;
                    if (teamsGD < 5 && teamsGD > 0)
                    {
                        team1GDScore = 1;
                    }
                    else if (teamsGD < 10)
                    {
                        team1GDScore = 2;
                    }
                    else if (teamsGD < 15)
                    {
                        team1GDScore = 3;
                    }
                    else
                    {
                        team1GDScore = 4;
                    }
                }
                else
                {
                    int teamsGD = team2GD - team1GD;
                    if (teamsGD < 5 && teamsGD > 0)
                    {
                        team2GDScore = 1;
                    }
                    else if (teamsGD < 10)
                    {
                        team2GDScore = 2;
                    }
                    else if (teamsGD < 15)
                    {
                        team2GDScore = 3;
                    }
                    else
                    {
                        team2GDScore = 4;
                    }
                }
            }

            // Adding all the values grabbed and sending them to the VM
            MatchProbabilityViewModel vm = new MatchProbabilityViewModel();
            vm.Team1 = team1;
            vm.Team2 = team2;
            vm.Team1Score = team1FormScore + team1RankScore + team1GDScore;
            vm.Team2Score = team2FormScore + team2RankScore + team2GDScore;
            return View(vm);
        }

        // Translates the team names between the two separate APIs
        public string ConvertTeamEnglish(string team)
        {
            switch (team)
            {
                case "Tottenham Hotspur FC":
                    team = "Tottenham";
                    break;
                case "Chelsea FC":
                    team = "Chelsea";
                    break;
                default:
                    break;
            }
            return team;
        }

        [HttpGet]
        public IActionResult Quiz1(string league, string season)
        {
            string[] questions = new string[3];

            string displayLeague = "";

            // Showing correct league names in question
            switch (league)
            {
                case "en.1":
                    displayLeague = "English";
                    break;
                case "es.1":
                    displayLeague = "Spanish";
                    break;
                case "de.1":
                    displayLeague = "German";
                    break;
                case "it.1":
                    displayLeague = "Italian";
                    break;
                case "fr.1":
                    displayLeague = "French";
                    break;
                default:
                    break;
            }

            questions[0] = "Which team won? Or was it a draw?";
            questions[1] = $"Which team placed first in the {displayLeague} league in the {season} season?";
            questions[2] = $"Which team placed last in the {displayLeague} league in the {season} season?";

            Random rndQuestion = new Random();
            int qIndex = rndQuestion.Next(questions.Length);

            if (qIndex == 0)
            {
                FootballMatches matches = FootballDAL.GetMatches(league, season);

                Random r = new Random();
                int index = r.Next(matches.matches.Count);

                Match match = matches.matches[index];

                // Ensures a match with a score is selected
                while (match.score == null)
                {
                    index = r.Next(matches.matches.Count);
                    match = matches.matches[index];
                }

                TempData["League"] = league;
                TempData["Season"] = season;
                TempData["MatchIndex"] = index;
                return View(match);
            }
            else if (qIndex == 1)
            {
                FootballStandings standings = FootballDAL.GetStandings(league, season);
                var teams = standings.response[0].league.standings[0];

                string correctAnswer = teams[0].team.name;

                Random rndIncorrect = new Random();
                int index = rndIncorrect.Next(1, teams.Length - 1);
                string firstIncorrect = teams[index].team.name;
                index = rndIncorrect.Next(1, teams.Length - 1);

                string secondIncorrect = teams[index].team.name;

                while (secondIncorrect == firstIncorrect)
                {
                    secondIncorrect = teams[index].team.name;
                    index = rndIncorrect.Next(1, teams.Length - 1);
                }

                string thirdIncorrect = teams[index].team.name;
                while (thirdIncorrect == firstIncorrect || thirdIncorrect == secondIncorrect)
                {
                    thirdIncorrect = teams[index].team.name;
                    index = rndIncorrect.Next(1, teams.Length - 1);
                }

                QuestionMultipleChoice options = new QuestionMultipleChoice();
                options.Question = questions[1];
                options.CorrectAnswer = correctAnswer;
                options.FirstIncorrect = firstIncorrect;
                options.SecondIncorrect = secondIncorrect;
                options.ThirdIncorrect = thirdIncorrect;

                return RedirectToAction("Quiz2", options);
            }
            else if (qIndex == 2)
            {
                FootballStandings standings = FootballDAL.GetStandings(league, season);
                var teams = standings.response[0].league.standings[0];

                string correctAnswer = teams.Last().team.name;

                Random rndIncorrect = new Random();
                int index = rndIncorrect.Next(1, teams.Length - 1);
                string firstIncorrect = teams[index].team.name;
                index = rndIncorrect.Next(1, teams.Length - 1);

                string secondIncorrect = teams[index].team.name;

                while (secondIncorrect == firstIncorrect)
                {
                    secondIncorrect = teams[index].team.name;
                    index = rndIncorrect.Next(1, teams.Length - 1);
                }

                string thirdIncorrect = teams[index].team.name;
                while (thirdIncorrect == firstIncorrect || thirdIncorrect == secondIncorrect)
                {
                    thirdIncorrect = teams[index].team.name;
                    index = rndIncorrect.Next(1, teams.Length - 1);
                }

                QuestionMultipleChoice options = new QuestionMultipleChoice();
                options.Question = questions[2];
                options.CorrectAnswer = correctAnswer;
                options.FirstIncorrect = firstIncorrect;
                options.SecondIncorrect = secondIncorrect;
                options.ThirdIncorrect = thirdIncorrect;

                return RedirectToAction("Quiz3", options);
            }

            return View();
        }

        public IActionResult Quiz2(QuestionMultipleChoice options)
        {
            return View(options);
        }

        public IActionResult Quiz3(QuestionMultipleChoice options)
        {
            return View(options);
        }

        [HttpPost]
        public IActionResult QuizResult(int index, string league, string season, string answer)
        {
            FootballMatches matches = FootballDAL.GetMatches(league, season);
            Match match = matches.matches[index];

            var winner = "";

            if (match.score.ft[0] > match.score.ft[1])
            {
                winner = "team1";
            }
            else if (match.score.ft[0] < match.score.ft[1])
            {
                winner = "team2";
            }
            else if (match.score.ft[0] == match.score.ft[1])
            {
                winner = "draw";
            }

            if (answer == winner)
            {
                ViewBag.Result = "Congratulations! You really know your football trivia.";
            }
            else
            {
                ViewBag.Result = "Sorry, you were incorrect. Better luck next time.";
            }
            return View(match);
        }

        public IActionResult Quiz2Result(List<string> randomAnswers, string answer, string correctAnswer, string question)
        {
            Quiz2ResultViewModel quiz2VM = new Quiz2ResultViewModel();
            quiz2VM.Answer = answer;
            quiz2VM.RandomAnswers = randomAnswers;
            quiz2VM.CorrectAnswer = correctAnswer;
            quiz2VM.Question = question;

            return View(quiz2VM);
        }

        public IActionResult Quiz3Result(List<string> randomAnswers, string answer, string correctAnswer, string question)
        {
            Quiz2ResultViewModel quiz2VM = new Quiz2ResultViewModel();
            quiz2VM.Answer = answer;
            quiz2VM.RandomAnswers = randomAnswers;
            quiz2VM.CorrectAnswer = correctAnswer;
            quiz2VM.Question = question;

            return View(quiz2VM);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
