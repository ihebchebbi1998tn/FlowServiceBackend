import { Node, Edge } from '@xyflow/react';
import { WorkflowNodeData } from '../types/workflowTypes';

export interface ValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
}

export class WorkflowValidator {
  static validateWorkflow(nodes: Node[], edges: Edge[]): ValidationResult {
    const errors: string[] = [];
    const warnings: string[] = [];

    // Check if workflow has at least one node
    if (nodes.length === 0) {
      errors.push("The workflow must contain at least one node");
    }

    // Check for trigger nodes (match both exact types and -trigger suffix pattern)
    const triggerNodes = nodes.filter(node => {
      const nodeData = node.data as any;
      const nodeType = nodeData.type || '';
      return ['trigger', 'webhook', 'scheduled'].includes(nodeType) || 
             nodeType.includes('-trigger') || 
             nodeType.includes('trigger') ||
             nodeData.isTrigger === true;
    });
    
    if (triggerNodes.length === 0) {
      warnings.push("It's recommended to have at least one trigger in the workflow");
    }

    // Check for orphaned nodes (nodes without incoming or outgoing connections)
    const connectedNodeIds = new Set();
    edges.forEach(edge => {
      connectedNodeIds.add(edge.source);
      connectedNodeIds.add(edge.target);
    });

    const orphanedNodes = nodes.filter(node => !connectedNodeIds.has(node.id));
    if (orphanedNodes.length > 0) {
      warnings.push(`${orphanedNodes.length} isolated node(s) detected: ${orphanedNodes.map(n => n.data.label).join(', ')}`);
    }

    // Check for cycles in non-loop nodes
    if (this.hasCycles(nodes, edges)) {
      errors.push("The workflow contains cycles which may cause infinite loops");
    }

    // Validate conditional nodes
    const conditionalNodes = nodes.filter(node => {
      const nodeData = node.data as any;
      return ['if-else', 'switch', 'condition'].includes(nodeData.type);
    });

    conditionalNodes.forEach(node => {
      const outgoingEdges = edges.filter(edge => edge.source === node.id);
      const nodeType = (node.data as any).type;
      
      if (nodeType === 'if-else' && outgoingEdges.length !== 2) {
        errors.push(`IF/ELSE node "${node.data.label}" must have exactly 2 outputs`);
      }
      
      if (nodeType === 'switch' && outgoingEdges.length < 2) {
        warnings.push(`SWITCH node "${node.data.label}" should have at least 2 outputs`);
      }
    });

    // Validate parallel nodes
    const parallelNodes = nodes.filter(node => {
      const nodeData = node.data as any;
      return nodeData.type === 'parallel';
    });

    parallelNodes.forEach(node => {
      const outgoingEdges = edges.filter(edge => edge.source === node.id);
      if (outgoingEdges.length < 2) {
        warnings.push(`Parallel node "${node.data.label}" should have at least 2 outputs`);
      }
    });

    // Check for unreachable nodes
    const reachableNodes = this.findReachableNodes(nodes, edges, triggerNodes);
    const unreachableNodes = nodes.filter(node => !reachableNodes.has(node.id));
    
    if (unreachableNodes.length > 0) {
      warnings.push(`${unreachableNodes.length} node(s) not reachable from triggers`);
    }

    // Validate node configurations
    nodes.forEach(node => {
      const nodeData = node.data as any;
      const configErrors = this.validateNodeConfiguration(nodeData);
      errors.push(...configErrors);
    });

    return {
      isValid: errors.length === 0,
      errors,
      warnings
    };
  }

  private static validateNodeConfiguration(nodeData: any): string[] {
    const errors: string[] = [];
    const config = nodeData.config;

    if (!config) {
      return errors;
    }

    // Validate email nodes
    if (nodeData.type.includes('email') && config.emailData) {
      if (!config.emailData.subject?.trim()) {
        errors.push(`Email node "${nodeData.label}" must have a subject`);
      }
    }

    // Validate API nodes
    if (nodeData.type === 'api' && config.apiData) {
      if (!config.apiData.url?.trim()) {
        errors.push(`API node "${nodeData.label}" must have a URL`);
      }
      
      try {
        new URL(config.apiData.url || '');
      } catch {
  errors.push(`API node "${nodeData.label}" has an invalid URL`);
      }
    }

    // Validate database nodes
    if (nodeData.type === 'database' && config.databaseData) {
      if (!config.databaseData.table?.trim()) {
        errors.push(`Database node "${nodeData.label}" must specify a table`);
      }
    }

    // Validate conditional nodes
    if (nodeData.type === 'condition' && config.conditionData) {
      if (!config.conditionData.field?.trim()) {
        errors.push(`Condition node "${nodeData.label}" must specify a field`);
      }
    }

    return errors;
  }

  private static hasCycles(nodes: Node[], edges: Edge[]): boolean {
    const visited = new Set<string>();
    const recursionStack = new Set<string>();

    const dfs = (nodeId: string): boolean => {
      if (recursionStack.has(nodeId)) {
        return true; // Cycle detected
      }
      
      if (visited.has(nodeId)) {
        return false;
      }

      visited.add(nodeId);
      recursionStack.add(nodeId);

      const outgoingEdges = edges.filter(edge => edge.source === nodeId);
      for (const edge of outgoingEdges) {
        if (dfs(edge.target)) {
          return true;
        }
      }

      recursionStack.delete(nodeId);
      return false;
    };

    for (const node of nodes) {
      if (!visited.has(node.id)) {
        if (dfs(node.id)) {
          return true;
        }
      }
    }

    return false;
  }

  private static findReachableNodes(nodes: Node[], edges: Edge[], startNodes: Node[]): Set<string> {
    const reachable = new Set<string>();
    const queue = [...startNodes.map(n => n.id)];

    while (queue.length > 0) {
      const nodeId = queue.shift()!;
      if (reachable.has(nodeId)) {
        continue;
      }

      reachable.add(nodeId);
      
      const outgoingEdges = edges.filter(edge => edge.source === nodeId);
      outgoingEdges.forEach(edge => {
        if (!reachable.has(edge.target)) {
          queue.push(edge.target);
        }
      });
    }

    return reachable;
  }
}