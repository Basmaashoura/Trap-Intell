using System;
using System.Collections.Generic;
using System.Linq;
using Trap_Intel.Domain.Abstractions;
using Trap_Intel.Domain.Shared;

namespace Trap_Intel.Domain.Reporting
{
    /// <summary>
    /// Report template aggregate for consistent report generation.
    /// Manages template structure and sections.
    /// </summary>
    public class ReportTemplate : AggregateRoot<Guid>
    {
        private readonly List<TemplateSection> _sections = new();

        private ReportTemplate() { }

        private ReportTemplate(
            Guid id,
            Guid? organizationId,
            Guid createdBy,
            ReportType type,
            TemplateName name,
            TemplateGuidelines guidelines)
            : base(id)
        {
            OrganizationId = organizationId;
            CreatedBy = createdBy;
            Type = type;
            Name = name;
            Guidelines = guidelines;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid? OrganizationId { get; }
        public Guid CreatedBy { get; }
        public ReportType Type { get; }
        public TemplateName Name { get; private set; }
        public TemplateGuidelines Guidelines { get; private set; }
        public IReadOnlyList<TemplateSection> Sections => _sections.AsReadOnly();
        public DateTime CreatedAt { get; }
        public DateTime UpdatedAt { get; private set; }

        /// <summary>Factory method to create template.</summary>
        public static Result<ReportTemplate> Create(
            Guid? organizationId,
            Guid createdBy,
            ReportType type,
            TemplateName name,
            TemplateGuidelines guidelines)
        {
            if (createdBy == Guid.Empty)
                return Result.Failure<ReportTemplate>(ReportingErrors.InvalidCreatedBy);

            var template = new ReportTemplate(
                Guid.NewGuid(),
                organizationId,
                createdBy,
                type,
                name,
                guidelines);

            template.RaiseDomainEvent(new TemplateCreatedEvent(
                template.Id, type, createdBy, DateTime.UtcNow));

            return Result.Success(template);
        }

        /// <summary>Add section to template.</summary>
        public Result AddSection(TemplateSection section)
        {
            if (section == null)
                return Result.Failure(ReportingErrors.InvalidTemplateSection);

            if (_sections.Any(s => s.Name == section.Name))
                return Result.Failure(ReportingErrors.DuplicateTemplateSectionName);

            _sections.Add(section);
            UpdatedAt = DateTime.UtcNow;

            RaiseDomainEvent(new TemplateSectionAddedEvent(
                Id, section.Name, DateTime.UtcNow));

            return Result.Success();
        }

        /// <summary>Remove section from template.</summary>
        public Result RemoveSection(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
                return Result.Failure(ReportingErrors.InvalidSectionName);

            var section = _sections.FirstOrDefault(s => s.Name == sectionName);
            if (section == null)
                return Result.Failure(ReportingErrors.SectionNotFound);

            _sections.Remove(section);
            UpdatedAt = DateTime.UtcNow;

            return Result.Success();
        }

        /// <summary>Validate template structure.</summary>
        public Result Validate()
        {
            var rule = new TemplateValidationRule(this);
            if (!rule.IsSatisfied())
                return Result.Failure(rule.Error);

            return Result.Success();
        }
    }

    /// <summary>Template section entity.</summary>
    public class TemplateSection : Entity<Guid>
    {
        public string Name { get; }
        public string Description { get; }
        public int Order { get; }

        private TemplateSection(string name, string description, int order)
            : base(Guid.NewGuid())
        {
            Name = name;
            Description = description;
            Order = order;
        }

        public static Result<TemplateSection> Create(
            string name, string description, int order)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure<TemplateSection>(ReportingErrors.InvalidSectionName);

            if (string.IsNullOrWhiteSpace(description))
                return Result.Failure<TemplateSection>(ReportingErrors.InvalidSectionDescription);

            if (order < 0)
                return Result.Failure<TemplateSection>(ReportingErrors.InvalidSectionOrder);

            return Result.Success(new TemplateSection(
                name.Trim(), description.Trim(), order));
        }
    }
}
