using MiskBeirut.Application.Services;

namespace MiskBeirut.Application.Managers;

/// <summary>
/// A job application was refused because of its CV file rather than its form data. Carries the
/// scanner's own verdict so the Web layer can tell the applicant the truth — "we found something in
/// this file" and "we couldn't check this file right now" are very different messages, and showing
/// the first when the second happened tells a candidate their perfectly good CV is infected.
/// </summary>
public class CvRejectedException : InvalidOperationException
{
    public CvSubmissionOutcome Outcome { get; }

    public CvRejectedException(CvSubmissionOutcome outcome, string message) : base(message)
    {
        Outcome = outcome;
    }
}
