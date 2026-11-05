using System.ComponentModel.DataAnnotations;

namespace HospitalManagement.Web.Models;

public class Appointment : ITimestamped
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient? Patient { get; set; }

    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    [DataType(DataType.Date)]
    public DateOnly AppointmentDate { get; set; }

    [DataType(DataType.Time)]
    public TimeOnly AppointmentTime { get; set; }

    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
