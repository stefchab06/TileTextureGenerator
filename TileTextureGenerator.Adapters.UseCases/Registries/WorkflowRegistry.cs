using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TileTextureGenerator.Adapters.UseCases.Ports.Input;
using TileTextureGenerator.Core.Entities;

namespace TileTextureGenerator.Adapters.UseCases.Registries;

/// <summary>
/// Attribute to mark which project type a workflow handles
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class WorkflowForAttribute : Attribute
{
    public Type ProjectType { get; }
    
    public WorkflowForAttribute(Type projectType)
    {
        if (!typeof(TileTextureProjectBase).IsAssignableFrom(projectType))
        {
            throw new ArgumentException(
                $"ProjectType must derive from TileTextureProjectBase. Got: {projectType.Name}",
                nameof(projectType));
        }
        
        ProjectType = projectType;
    }
}

/// <summary>
/// Registry to map project types to their workflows
/// Auto-registers workflows marked with [WorkflowFor] attribute
/// </summary>
public static class WorkflowRegistry
{
    private static readonly Dictionary<Type, IProjectWorkflow> _workflows = new();

    /// <summary>
    /// Manually register a workflow for a project type
    /// </summary>
    public static void Register<TProject>(IProjectWorkflow workflow) 
        where TProject : TileTextureProjectBase
    {
        _workflows[typeof(TProject)] = workflow;
    }

    /// <summary>
    /// Get the workflow for a specific project instance
    /// </summary>
    public static IProjectWorkflow GetWorkflow(TileTextureProjectBase project)
    {
        var projectType = project.GetType();
        
        if (_workflows.TryGetValue(projectType, out var workflow))
            return workflow;
            
        throw new InvalidOperationException(
            $"No workflow registered for project type: {projectType.Name}. " +
            $"Available workflows: {string.Join(", ", _workflows.Keys.Select(t => t.Name))}");
    }

    /// <summary>
    /// Auto-register all workflows in the given assembly that have [WorkflowFor] attribute
    /// </summary>
    public static void ForceAutoRegistration(Assembly assembly, IServiceProvider services)
    {
        var workflowTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && 
                   typeof(IProjectWorkflow).IsAssignableFrom(t));

        foreach (var workflowType in workflowTypes)
        {
            var attr = workflowType.GetCustomAttribute<WorkflowForAttribute>();
            if (attr != null)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    $"[WorkflowRegistry] Registering {workflowType.Name} for {attr.ProjectType.Name}");
#endif
                var workflow = (IProjectWorkflow)ActivatorUtilities.CreateInstance(services, workflowType);
                _workflows[attr.ProjectType] = workflow;
            }
        }
    }

    /// <summary>
    /// Check if a workflow is registered for a project type
    /// </summary>
    public static bool IsRegistered<TProject>() where TProject : TileTextureProjectBase
    {
        return _workflows.ContainsKey(typeof(TProject));
    }
}
