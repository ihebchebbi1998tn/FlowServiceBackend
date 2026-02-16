import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Code, Key, Book, Copy, ExternalLink } from "lucide-react";
import { useTranslation } from "react-i18next";

const apiEndpoints = [
  { 
    method: "GET", 
    endpoint: "/api/contacts", 
    description: "Retrieve all contacts with filtering and pagination", 
    status: "Active",
    example: "?page=1&pageSize=10&search=john&status=active"
  },
  { 
    method: "GET", 
    endpoint: "/api/contacts/{id}", 
    description: "Get single contact by ID", 
    status: "Active",
    example: "/api/contacts/123"
  },
  { 
    method: "POST", 
    endpoint: "/api/contacts", 
    description: "Create new contact", 
    status: "Active",
    example: "JSON body: { firstName, lastName, email, phone, company }"
  },
  { 
    method: "PUT", 
    endpoint: "/api/contacts/{id}", 
    description: "Update existing contact", 
    status: "Active",
    example: "/api/contacts/123 with JSON body"
  },
  { 
    method: "DELETE", 
    endpoint: "/api/contacts/{id}", 
    description: "Delete contact (soft delete)", 
    status: "Active",
    example: "/api/contacts/123"
  },
  { 
    method: "GET", 
    endpoint: "/api/contacts/search", 
    description: "Search contacts with advanced filters", 
    status: "Active",
    example: "?query=john&type=person&status=active"
  },
  { 
    method: "POST", 
    endpoint: "/api/contacts/import", 
    description: "Bulk import contacts from CSV/Excel", 
    status: "Active",
    example: "Multipart form with file upload"
  },
  { 
    method: "GET", 
    endpoint: "/api/contacttags", 
    description: "Retrieve all contact tags", 
    status: "Active",
    example: "?page=1&pageSize=50"
  },
  { 
    method: "POST", 
    endpoint: "/api/contacttags", 
    description: "Create new contact tag", 
    status: "Active",
    example: "JSON body: { name, color, description }"
  },
  { 
    method: "GET", 
    endpoint: "/api/contactnotes/contact/{contactId}", 
    description: "Get all notes for a specific contact", 
    status: "Active",
    example: "/api/contactnotes/contact/123"
  },
  { 
    method: "POST", 
    endpoint: "/api/contactnotes", 
    description: "Create new contact note", 
    status: "Active",
    example: "JSON body: { contactId, content, type }"
  },
  { 
    method: "POST", 
    endpoint: "/api/Auth/login", 
    description: "User authentication", 
    status: "Active",
    example: "JSON body: { email, password }"
  },
  { 
    method: "POST", 
    endpoint: "/api/Auth/signup", 
    description: "User registration", 
    status: "Active",
    example: "JSON body: { email, password, firstName, lastName }"
  },
  { 
    method: "GET", 
    endpoint: "/api/Preferences", 
    description: "Get user preferences", 
    status: "Active",
    example: "Requires Bearer token authentication"
  },
  { 
    method: "GET", 
    endpoint: "/api/lookups/{lookupType}", 
    description: "Get lookup data by type", 
    status: "Active",
    example: "/api/lookups/priorities or /api/lookups/statuses"
  }
];

export function ApiDocumentation() {
  const { t } = useTranslation();

  return (
    <div className="space-y-6">
      {/* API Keys Management */}
      <Card className="border-border/50 shadow-soft">
        <CardHeader>
          <CardTitle className="text-foreground flex items-center gap-2">
            <Key className="h-5 w-5" />
            {t('apiDocumentation.apiKeysTitle', 'API Keys Management')}
          </CardTitle>
          <CardDescription>{t('apiDocumentation.apiKeysDesc', 'Generate and manage API keys for external integrations')}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="apiKey">{t('apiDocumentation.currentApiKey', 'Current API Key')}</Label>
            <div className="flex gap-2">
              <Input 
                id="apiKey" 
                value="fk_live_51H8Z3K2eZvKYlo2CNkHvZpP9w8QvYzJ9h4Q7F2J8kJ0O3F5G6H7I8J9K0L1M2N3" 
                readOnly 
                className="font-mono text-sm"
              />
              <Button variant="outline" size="icon">
                <Copy className="h-4 w-4" />
              </Button>
            </div>
          </div>
          <div className="flex gap-2">
            <Button variant="outline">{t('apiDocumentation.generateKey', 'Generate New Key')}</Button>
            <Button variant="destructive">{t('apiDocumentation.revokeKey', 'Revoke Key')}</Button>
          </div>
          <div className="p-4 bg-warning/10 border border-warning/20 rounded-lg">
            <p className="text-sm text-foreground">
              <strong>{t('apiDocumentation.securityNoticeTitle', 'Security Notice:')}</strong> {t('apiDocumentation.securityNoticeDesc', 'Keep your API keys secure and never expose them in client-side code.')}
            </p>
          </div>
        </CardContent>
      </Card>

      {/* API Documentation */}
      <Card className="border-border/50 shadow-soft">
        <CardHeader>
          <CardTitle className="text-foreground flex items-center gap-2">
            <Book className="h-5 w-5" />
            {t('apiDocumentation.title', 'API Documentation')}
          </CardTitle>
          <CardDescription>{t('apiDocumentation.desc', 'Available API endpoints and their documentation')}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex justify-between items-center">
            <p className="text-sm text-muted-foreground">
              {t('apiDocumentation.baseUrl', 'Base URL')}: https://co-mouhibilpadrino.com
            </p>
            <Button 
              variant="outline" 
              size="sm" 
              className="flex items-center gap-2"
              onClick={() => window.open('https://co-mouhibilpadrino.com/index.html', '_blank')}
            >
              <ExternalLink className="h-4 w-4" />
              {t('apiDocumentation.fullDocumentation', 'Full Documentation')}
            </Button>
          </div>
          
          <div className="space-y-2">
            {apiEndpoints.map((endpoint, index) => (
              <div key={index} className="p-4 border border-border/50 rounded-lg">
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-3">
                    <Badge variant={
                      endpoint.method === "GET" ? "default" : 
                      endpoint.method === "POST" ? "secondary" : 
                      endpoint.method === "PUT" ? "outline" :
                      endpoint.method === "DELETE" ? "destructive" : "outline"
                    }>
                      {endpoint.method}
                    </Badge>
                    <code className="text-sm font-mono text-foreground">{endpoint.endpoint}</code>
                  </div>
                  <Badge variant="outline" className="text-success border-success/20">
                    {endpoint.status}
                  </Badge>
                </div>
                <p className="text-sm text-muted-foreground mb-2">{endpoint.description}</p>
                <div className="text-xs text-muted-foreground bg-muted/30 p-2 rounded border">
                  <strong>{t('apiDocumentation.exampleLabel', 'Example:')}</strong> <code>{endpoint.example}</code>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Integration Settings */}
      <Card className="border-border/50 shadow-soft">
        <CardHeader>
          <CardTitle className="text-foreground flex items-center gap-2">
            <Code className="h-5 w-5" />
            {t('apiDocumentation.integrationsTitle', 'Integrations')}
          </CardTitle>
          <CardDescription>{t('apiDocumentation.integrationsDesc', 'Connect with third-party services')}</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="flex items-center justify-between p-4 rounded-lg border border-border/50">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 bg-primary/10 rounded-lg flex items-center justify-center">
                  <span className="text-primary font-bold">G</span>
                </div>
                <div>
                  <p className="font-medium text-foreground">Google Workspace</p>
                  <p className="text-sm text-muted-foreground">Gmail, Calendar, Drive</p>
                </div>
              </div>
              <Button variant="outline" size="sm">{t('apiDocumentation.configure', 'Configure')}</Button>
            </div>
            <div className="flex items-center justify-between p-4 rounded-lg border border-border/50">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 bg-blue-600/10 rounded-lg flex items-center justify-center">
                  <span className="text-blue-700 font-bold">O</span>
                </div>
                <div>
                  <p className="font-medium text-foreground">Outlook</p>
                  <p className="text-sm text-muted-foreground">Email, Calendar</p>
                </div>
              </div>
              <Button variant="outline" size="sm">{t('apiDocumentation.configure', 'Configure')}</Button>
            </div>
            <div className="flex items-center justify-between p-4 rounded-lg border border-border/50">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 bg-purple-500/10 rounded-lg flex items-center justify-center">
                  <span className="text-purple-600 font-bold">S</span>
                </div>
                <div>
                  <p className="font-medium text-foreground">Slack</p>
                  <p className="text-sm text-muted-foreground">Team communication</p>
                </div>
              </div>
              <Button variant="outline" size="sm">{t('apiDocumentation.configure', 'Configure')}</Button>
            </div>
            <div className="flex items-center justify-between p-4 rounded-lg border border-border/50">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 bg-green-500/10 rounded-lg flex items-center justify-center">
                  <span className="text-green-600 font-bold">Z</span>
                </div>
                <div>
                  <p className="font-medium text-foreground">Zapier</p>
                  <p className="text-sm text-muted-foreground">Workflow automation</p>
                </div>
              </div>
              <Button variant="outline" size="sm">{t('apiDocumentation.configure', 'Configure')}</Button>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
