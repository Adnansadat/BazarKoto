import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-privacy-policy-page',
  standalone: true,
  imports: [TranslateModule],
  templateUrl: './privacy-policy-page.component.html',
  styleUrl: './privacy-policy-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrivacyPolicyPageComponent {
  private readonly privacyMailtoUrl = 'mailto:support@bazarkoto.com?subject=Privacy%20Inquiry%20-%20BazarKoto';
  private readonly privacyGmailComposeUrl =
    'https://mail.google.com/mail/?view=cm&fs=1&to=support@bazarkoto.com&su=Privacy%20Inquiry%20-%20BazarKoto';

  openPrivacyEmail(event: MouseEvent): void {
    event.preventDefault();

    if (this.isMobileOrTablet()) {
      window.location.href = this.privacyMailtoUrl;
      return;
    }

    window.open(this.privacyGmailComposeUrl, '_blank', 'noopener,noreferrer');
  }

  private isMobileOrTablet(): boolean {
    return /Android|iPhone|iPad|iPod|Mobile|Tablet/i.test(window.navigator.userAgent);
  }
}
