<?xml version="1.0" encoding="ISO-8859-1"?>
<!-- Edited by XMLSpy� -->
<xsl:stylesheet version="1.0"
xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:template match="/">
		<html>
			<body>
				<center>
					<h1>Selenium Test Automation Report for {0} in {1} application</h1>
					<table border="1" border-color="black" cellspacing="0" cellpadding="2">
						<tr bgcolor="#9acd32">
							<th>Total Test cases Run</th>
							<th>Total Test cases Passed</th>
							<th bgcolor="#9acd32">Total Test cases Failed</th>
						</tr>
						<tr>
							<td>
								<xsl:value-of select="count(//testcaseid)"/>
							</td>
							<td>
								<xsl:value-of select="count(//success[text()='Pass'])"/>
							</td>
							<td>
								<xsl:value-of select="count(//success[text()='Fail'])"/>
							</td>
						</tr>
					</table>
					<p>
					</p>
					<p>
					</p>
					<p>
					</p>
					<p>
					</p>
					<p>
					</p>
					<p>
					</p>
					<p>
					</p>
					<p>
					</p>
					<p>
					</p>
					<p>
					</p>
					<p>
					</p>
					<p>
					</p>
					<table border="1" border-color="black" cellspacing="0" cellpadding="2">
						<tr bgcolor="#9acd32">
							<th>Scenario Id</th>
							<th>Mode</th>
							<th>Script</th>
							<th>Summary</th>
							<th>Date</th>
							<th>Status</th>
							<th>Message</th>
						</tr>
						<xsl:for-each select="root/result">
							<tr >
								<td>
									<b>
										<xsl:value-of select="testcaseid" />
									</b>
								</td>
								<td>
									<b>
										<xsl:value-of select="mode" />
									</b>
								</td>
								<td>
									<b>
										<xsl:value-of select="scriptname" />
									</b>
								</td>
								<td>
									<b>
										<xsl:value-of select="summary" />
									</b>
								</td>
								<td>
									<b>
										<xsl:value-of select="date" />
									</b>
								</td>
								<xsl:choose>
									<xsl:when test="success='Fail'">
										<td bgcolor="red">
											<b>
												<xsl:value-of select="status"/>
											</b>
										</td>
									</xsl:when>
									<xsl:otherwise>
										<td>
											<b>
												<xsl:value-of select="status"/>
											</b>
										</td>
									</xsl:otherwise>
								</xsl:choose>
								<td>
									<b>
										<xsl:value-of select="message" />
									</b>
								</td>
							</tr>
						</xsl:for-each>
					</table>
				</center>
			</body>
		</html>
	</xsl:template>
</xsl:stylesheet>
